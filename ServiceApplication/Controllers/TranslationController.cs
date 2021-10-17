using BrandShield.Common;
using log4net;
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
        private readonly ILog _logger;

        public TranslationController(ITranslationService translationService, ILog logger)
        {
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        // POST api/Translation, body: [{"Id": 1}, {"Id": 2}]
        public object Post([FromBody] IEnumerable<TranslationTask> translationTasks)
        {
            var count = translationTasks.Count();
            _logger.InfoFormat("Got request with {0} translation tasks", count);
            if (translationTasks == null || !translationTasks.Any())
            {
                return this.BadRequest();
            }

            if (translationTasks.Count() > CommonConsts.MAX_TASKS_PER_REQUEST)
            {
                return this.BadRequest($"The request is too big (max tasks {CommonConsts.MAX_TASKS_PER_REQUEST})");
            }

            var retVal = _translationService.Process(translationTasks);

            _logger.InfoFormat("Return '{0}' for request with {0} translation tasks", retVal, count);
            return retVal;
        }

        // DELETE api/Translation/A300DCB9-F773-474A-9D73-CB8CE62BD2F8
        public async Task Delete(Guid id)
        {
            _logger.InfoFormat("Got stop-request for processing with id '{0}'", id);
            await _translationService.CompleteAsync(id);
        }
    }
}
