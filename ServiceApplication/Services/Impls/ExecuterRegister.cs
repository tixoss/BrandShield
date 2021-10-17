using BrandShield.Common;
using log4net;
using ServiceApplication.Services.Impls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public class ExecuterRegister : IExecuterRegister
    {
        private static readonly object _lock = new object();
        private static IExecuterRegister _instance = null;

        private readonly ConcurrentDictionary<Guid, Executor> _executors = new ConcurrentDictionary<Guid, Executor>();
        private readonly ICloudExecuterFactory _cloudExecuterFactory;
        private readonly ILog _log;
        private readonly Dictionary<Guid, (CancellationTokenSource, Task)> _dic = new Dictionary<Guid, (CancellationTokenSource, Task)>();

        internal ExecuterRegister(ICloudExecuterFactory cloudExecuterFactory, ILog log)
        {
            _cloudExecuterFactory = cloudExecuterFactory ?? throw new ArgumentNullException(nameof(cloudExecuterFactory));
            _log = log;
        }

        public static IExecuterRegister GetInstance(ICloudExecuterFactory cloudExecuterFactory, ILog log)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = _instance ?? new ExecuterRegister(cloudExecuterFactory, log);
                }
            }

            return _instance;
        }

        public void Place(Guid executionId, TranslationTask[] translationTasks)
        {
            _log.InfoFormat("Place {0} translation tasks, current count of machine {1}.", translationTasks.Length, _executors.Count);

            var cts = new CancellationTokenSource();
            var ctoken = cts.Token;

            Task processTask = Task.Factory.StartNew(async (transTasks) =>
            {
                var asEnumerable = transTasks as IEnumerable<TranslationTask>;
                var rest = SendToExists(asEnumerable, ctoken);

                if (ctoken.IsCancellationRequested || !rest.Any()) { return executionId; }

                rest = await SendToNewExecutors(asEnumerable, ctoken).ConfigureAwait(false);

                if (ctoken.IsCancellationRequested || !rest.Any()) { return executionId; }

                SendRest(rest, ctoken);

                return executionId;

            }, translationTasks, ctoken)
                .Unwrap()
                .ContinueWith(t => {
                    // TODO FSY: If success
                    var execId = t.Result;
                    _dic.Remove(executionId);
                });

            _dic.Add(executionId, (cts, processTask));
        }

        public async Task Break(Guid executionId)
        {
            if (_dic.TryGetValue(executionId, out (CancellationTokenSource, Task) tulpe))
            {
                (CancellationTokenSource cts, Task task) = tulpe;
                cts.Cancel();
                await task;
            }
        }

        private IEnumerable<TranslationTask> SendToExists(IEnumerable<TranslationTask> asEnumerable, CancellationToken ctoken)
        {
            foreach (var executor in _executors.Values)
            {
                if (ctoken.IsCancellationRequested) { return Enumerable.Empty<TranslationTask>(); }
                lock (executor)
                {
                    if (executor.Capacity > 0)
                    {
                        var takeTransTasks = asEnumerable.Take(executor.Capacity).ToArray();
                        asEnumerable = asEnumerable.Skip(executor.Capacity);
                        executor.AddTasks(takeTransTasks, ctoken);
                    }
                }
            }

            return asEnumerable;
        }

        private async Task<IEnumerable<TranslationTask>> SendToNewExecutors(IEnumerable<TranslationTask> asEnumerable, CancellationToken ctoken)
        {
            var rest = asEnumerable.ToArray();
            var index = 0;
            while (rest.Length > 0)
            {
                if (ctoken.IsCancellationRequested) { return Enumerable.Empty<TranslationTask>(); }
                var newConnection = await _cloudExecuterFactory.GetNewAsync().ConfigureAwait(false);
                if (newConnection != null)
                {
                    var takeTransTasks = rest.Take(Consts.TASK_COUNT_PER_EXECUTER);
                    rest = rest.Skip(Consts.TASK_COUNT_PER_EXECUTER).ToArray();
                    index += Consts.TASK_COUNT_PER_EXECUTER;

                    var newExecutor = new Executor(newConnection, async (executorGuid) => await RemoveExecutor(executorGuid));
                    newExecutor.AddTasks(takeTransTasks, ctoken);
                    _executors.TryAdd(newExecutor.Id, newExecutor);
                }
                else
                {
                    break;
                }
            }

            return rest;
        }

        private async Task RemoveExecutor(Guid executorGuid)
        {
            if (_executors.TryRemove(executorGuid, out var executor))
            {
                await executor.DieAsync();
            }
        }

        private void SendRest(IEnumerable<TranslationTask> asEnumerable, CancellationToken ctoken)
        {
            ParallelOptions po = new ParallelOptions
            {
                CancellationToken = ctoken
            };

            IEnumerable<TranslationTask>[] t = asEnumerable.Split(_executors.Count).ToArray();
            var restAfterNew = new ConcurrentQueue<IEnumerable<TranslationTask>>(t);

            if (ctoken.IsCancellationRequested) { return; }

            Parallel.ForEach(_executors.Values, po, (executor) =>
            {
                if (ctoken.IsCancellationRequested) { return; }
                lock (executor)
                {
                    if (restAfterNew.TryDequeue(out IEnumerable<TranslationTask> batch))
                    {
                        executor.AddTasks(batch, ctoken);
                    }
                }
            });
        }
    }
}
