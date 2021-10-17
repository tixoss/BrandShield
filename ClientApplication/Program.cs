using BrandShield.Common;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonConsts = BrandShield.Common.Consts;

namespace BrandShield.ClientApplication
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (paramsParsed, countOfParallel) = ParseParams(args);
            if (!paramsParsed) { return; }

            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;

            var workers = new List<Task>();

            var mainTask = Task.Factory.StartNew(() =>
            {
                for (var i = 1; i <= countOfParallel; i++)
                {
                    Console.WriteLine($"Start worker {i,5}.");

                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var task = Task.Factory.StartNew(async (state) =>
                    {
                        var id = (int)state;
                        var rnd = new ThreadSafeRandom();
                        var client = new FakeClient(id);

                        while (!token.IsCancellationRequested)
                        {
                            var taskCount = rnd.Next(1, CommonConsts.MAX_TASKS_PER_REQUEST + 1);
                            var delay = rnd.Next(Consts.MIN_DELAY_OF_BREAK, Consts.MAX_DELAY_OF_BREAK + 1);

                            Console.WriteLine($"Worker {id,5}: Send request with {taskCount,6} tasks and stop after {delay,3} seconds.");

                            await PostWaitDelete(client, taskCount, delay, token);
                        }
                    }, i, TaskCreationOptions.LongRunning);

                    workers.Add(task);
                }
            });

            Console.ReadKey();

            Console.WriteLine($"{System.Environment.NewLine}-  Stopping");
            source.Cancel();

            await mainTask;
            await Task.WhenAll(workers);

            Console.WriteLine($"{System.Environment.NewLine}-  Completed");
        }

        private static async Task PostWaitDelete(IClient client, int taskCount, int delaySec, CancellationToken ct)
        {
            var tasks = Enumerable.Range(1, taskCount).Select(x => new TranslationTask(x));
            var executionId = await client.PostTranslationTasks(tasks).ConfigureAwait(false);

            await Task.Delay(new TimeSpan(0, 0, delaySec), ct).ConfigureAwait(false);

            await client.DeleteTranslationTasks(executionId).ConfigureAwait(false);
        }

        private static (bool, int) ParseParams(string[] args)
        {
            (bool, int) errorResult = (false, default(int));
            int? count = default;
            bool showHelp = false;

            var optionSet = new OptionSet() {
                {
                    "w|workers=",
                    $"the {{COUNT}} of workers.",
                    (int v) => count = v },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extra;
            try
            {
                extra = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                WriteError(e.Message);
                return errorResult;
            }

            if (showHelp)
            {
                Console.WriteLine("The application posts translation requests then wait for a timeout and break the translation process. To break the application press any key.");
                optionSet.WriteOptionDescriptions(Console.Out);
                return errorResult;
            }

            if (!count.HasValue || count.Value < 0)
            {
                WriteError("The {{COUNT}} of workers is undefined or less 0.");
                return (false, default);
            }

            return (true, count.Value);
        }

        private static void WriteError(string errorMessage)
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine("Try `ClientApplication --help' for more information.");
        }

        internal class ThreadSafeRandom
        {
            private static readonly Random _global = new Random();
            [ThreadStatic] private static Random _local;

            public int Next(int minValue, int maxValue)
            {
                if (_local == null)
                {
                    int seed;
                    lock (_global)
                    {
                        seed = _global.Next(minValue, maxValue);
                    }
                    _local = new Random(seed);
                }

                return _local.Next(minValue, maxValue);
            }
        }

    }
}
