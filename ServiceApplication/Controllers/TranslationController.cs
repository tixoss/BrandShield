using BrandShield.Common;
using ServiceApplication.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using CommonConsts = BrandShield.Common.Consts;

namespace ServiceApplication.Controllers
{
    public class TranslationController : ApiController
    {
        private readonly ITranslationService _translationService;

        public TranslationController(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        // POST api/Translation, body: [{"Id": 1}, {"Id": 2}]
        public async Task<object> Post([FromBody] IEnumerable<TranslationTask> translationTasks)
        {
            if (translationTasks == null || !translationTasks.Any())
            {
                return this.BadRequest();
            }

            if (translationTasks.Count() > CommonConsts.MAX_TASKS_PER_REQUEST)
            {
                return this.BadRequest($"The request is too big (max tasks {CommonConsts.MAX_TASKS_PER_REQUEST})");
            }

            var retVal = await _translationService.ProcessAsync(translationTasks);
            return retVal;
        }

        // DELETE api/Translation/A300DCB9-F773-474A-9D73-CB8CE62BD2F8
        public async Task Delete(Guid id)
        {
            await _translationService.CompleteAsync(id);
        }
    }
}
