using BrandShield.ClientApplication;
using BrandShield.Common;
using NDesk.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonConsts = BrandShield.Common.Consts;

namespace ClientApplication
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (paramsParsed, taskCount, delaySec) = ParseParams(args);
            if (!paramsParsed) { return; }

            await Execute(taskCount, delaySec);

            Console.WriteLine("-= Press any key =-");
            Console.ReadKey();
        }

        private static async Task Execute(int taskCount, int delaySec)
        {
            var client = new FakeClient();

            var tasks = Enumerable.Range(1, taskCount).Select(x => new TranslationTask(x));
            var executionId = await client.PostTranslationTasks(tasks).ConfigureAwait(false);

            await Task.Delay(new TimeSpan(0, 0, delaySec)).ConfigureAwait(false);

            await client.DeleteTranslationTasks(executionId).ConfigureAwait(false);
        }

        private static (bool, int, int) ParseParams(string[] args)
        {
            (bool, int, int) errorResult = (false, default(int), default(int));
            int? count = default;
            int? deplay = default;
            bool showHelp = false;

            var optionSet = new OptionSet() {
                {
                    "t|tasks=", 
                    $"the {{COUNT}} of tasks. Max={CommonConsts.MAX_TASKS_PER_REQUEST}", 
                    (int v) => count = v },
                {
                    "d|delay=",
                    $"the {{DELAY}} in seconds of sending DELETE-request to break execution. Min={Consts.MIN_DELAY_OF_BREAK}, Max={Consts.MAX_DELAY_OF_BREAK}.",
                    (int v) => deplay = v },
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
                optionSet.WriteOptionDescriptions(Console.Out);
                return errorResult;
            }

            Random rnd = null;
            var (countValid, countValue) = NormalizeCountParam(count, ref rnd);
            if (!countValid) { return errorResult; }

            var (delayValid, delayValue) = NormalizeDelayParam(deplay, ref rnd);
            if (!countValid) { return errorResult; }

            return (true, countValue, delayValue);
        }

        private static (bool, int) NormalizeCountParam(int? count, ref Random rnd)
        {
            if (!count.HasValue)
            {
                rnd = rnd ?? new Random();
                count = rnd.Next(1, CommonConsts.MAX_TASKS_PER_REQUEST + 1);
                Console.WriteLine($"Use random count of tasks: {count}");
                return (true, count.Value);
            }
            
            if (count.Value < 1 || count.Value > CommonConsts.MAX_TASKS_PER_REQUEST)
            {
                WriteError("The count of tasks is invalid.");
                return (false, default);
            }

            return (true, count.Value);
        }

        private static (bool, int) NormalizeDelayParam(int? delay, ref Random rnd)
        {
            if (!delay.HasValue)
            {
                rnd = rnd ?? new Random();
                delay = rnd.Next(Consts.MIN_DELAY_OF_BREAK, Consts.MAX_DELAY_OF_BREAK + 1);
                Console.WriteLine($"Use random delay: {delay}");
                return (true, delay.Value);
            }

            if (delay.Value < Consts.MIN_DELAY_OF_BREAK || delay.Value > Consts.MAX_DELAY_OF_BREAK)
            {
                WriteError("The delay is invalid.");
                return (false, default);
            }

            return (true, delay.Value);
        }

        private static void WriteError(string errorMessage)
        {
            Console.WriteLine(errorMessage);
            Console.WriteLine("Try `ClientApplication --help' for more information.");
        }
    }
}
