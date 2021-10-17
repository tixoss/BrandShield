using BrandShield.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApplication.Services.Impls
{
    public class Executor
    {
        private readonly object _lock = new object();

        private readonly IExecuterConnection _executerConnection;
        private readonly Action<Guid> _freeCallback;
        private readonly LinkedList<Task<int>> _tasks = new LinkedList<Task<int>>();
        private int _taskCount = 0;
        private ExecutorState _executorState = ExecutorState.Work;

        private Task _monitorTask;

        public Executor(IExecuterConnection executerConnection, Action<Guid> freeCallback)
        {
            _executerConnection = executerConnection ?? throw new ArgumentNullException(nameof(executerConnection));
            _freeCallback = freeCallback ?? throw new ArgumentNullException(nameof(freeCallback));
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }

        public int Capacity
        {
            get
            {
                var retVal = Consts.TASK_COUNT_PER_EXECUTER - _taskCount;
                if (retVal < 0 || _executorState == ExecutorState.Die)
                {
                    return 0;
                }

                return retVal;
            }
        }

        public void AddTask(TranslationTask translationTask, CancellationToken ct)
        {
            lock (_lock)
            {
                _taskCount++;
            }
            
            var task = Task.Factory.StartNew(async () =>
            {
                await _executerConnection.ExecuteAsync(translationTask, ct).ConfigureAwait(false);

                return 1;
            }).Unwrap();

            lock (_lock)
            {
                _tasks.AddLast(task);
            }

            StartMonitor();
        }

        public void AddTasks(IEnumerable<TranslationTask> translationTasks, CancellationToken ct)
        {
            var asArray = translationTasks.ToArray();
            lock (_lock)
            {
                _taskCount += asArray.Length;
            }

            var task = Task.Factory.StartNew(async () =>
            {
                await _executerConnection.ExecuteAsync(asArray, ct).ConfigureAwait(false);
                return asArray.Length;
            }).Unwrap();

            lock (_lock)
            {
                _tasks.AddLast(task);
            }

            StartMonitor();
        }

        public Task DieAsync()
        {
            return _executerConnection.DieAsync();
        }

        private void StartMonitor()
        {
            if (_monitorTask == null || _monitorTask.IsCompleted)
            {
                lock (_lock)
                {
                    if (_monitorTask == null || _monitorTask.IsCompleted)
                    {
                        _monitorTask = Task.Factory.StartNew(async () => await CheckTasks()).Unwrap();
                    }
                }
            }
        }

        private async Task CheckTasks()
        {
            while (_tasks.First != null)
            {
                await Task.WhenAny(_tasks).ConfigureAwait(false);

                LinkedListNode<Task<int>> nextNode = _tasks.First;
                while (nextNode != null)
                {
                    if (nextNode.Value.IsCompleted)
                    {
                        var removeNode = nextNode;
                        lock (_lock)
                        {
                            nextNode = nextNode.Next;
                            _tasks.Remove(removeNode);
                            _taskCount -= removeNode.Value.Result;
                        }
                    }
                    else
                    {
                        nextNode = nextNode.Next;
                    }
                }

                // TODO: Timeout
                await Task.Delay(100).ConfigureAwait(false);

                lock (_lock)
                {
                    if (_taskCount == 0)
                    {
                        _executorState = ExecutorState.Die;
                        _freeCallback(Id);

                        return;
                    }
                }
            }
        }

        internal enum ExecutorState
        {
            Work,
            Die
        }
    }
}
