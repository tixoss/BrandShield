using BrandShield.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly IExecuterRegister _executerRegister;

        public TranslationService(IExecuterRegister executerRegister)
        {
            _executerRegister = executerRegister ?? throw new ArgumentNullException(nameof(executerRegister));
        }

        /// <summary>
        /// Place request to processing
        /// </summary>
        /// <param name="request"></param>
        /// <returns>Id of processing</returns>
        public Guid Process(IEnumerable<TranslationTask> translationTasks)
        {
            var executionId = Guid.NewGuid();

            _executerRegister.Place(executionId, translationTasks.ToArray());

            return executionId;
        }

        public async Task CompleteAsync(Guid executionId)
        {
            await _executerRegister.Break(executionId).ConfigureAwait(false);
        }
    }
}
