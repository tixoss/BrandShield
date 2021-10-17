using BrandShield.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface ITranslationService
    {
        Guid Process(IEnumerable<TranslationTask> translationTasks);
        Task CompleteAsync(Guid executionId);
    }
}
