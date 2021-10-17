using BrandShield.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface IExecuterConnection
    {
        Task ExecuteAsync(IEnumerable<TranslationTask> translationTasks);
        Task ExecuteAsync(TranslationTask translationTask);
        Task DieAsync();
    }

    public static class ExecuterConnectionExtensions
    {
        public static void Execute(this IExecuterConnection connection, IEnumerable<TranslationTask> translationTasks)
        {
            connection.ExecuteAsync(translationTasks).Wait();
        }

        public static void Execute(this IExecuterConnection connection, TranslationTask translationTask)
        {
            connection.ExecuteAsync(translationTask).Wait();
        }

        public static void Die(this IExecuterConnection connection)
        {
            connection.DieAsync().Wait();
        }
    }
}
