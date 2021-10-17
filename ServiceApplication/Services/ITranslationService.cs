using BrandShield.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface ITranslationService
    {
        Task<Guid> ProcessAsync(IEnumerable<TranslationTask> translationTasks);
        Task CompleteAsync(Guid executionId);
    }

    public static class TranslationServiceExtensions
    {
        public static Guid Process(this ITranslationService service, IEnumerable<TranslationTask> translationTasks)
        {
            return service.ProcessAsync(translationTasks).Result;
        }

        public static void Complete(this ITranslationService service, Guid executionId)
        {
            service.CompleteAsync(executionId).Wait();
        }
    }

    public class FakeTranslationService : ITranslationService
    {
        public async Task CompleteAsync(Guid executionId)
        {
            Console.WriteLine($"CompleteAsync got executionId = {executionId}");
            await Task.Delay(0);
        }

        public async Task<Guid> ProcessAsync(IEnumerable<TranslationTask> translationTasks)
        {
            Console.WriteLine($"ProcessAsync got translationTasks = {translationTasks.Count()} tasks");
            await Task.Delay(0);
            return Guid.NewGuid();
        }
    }
}
