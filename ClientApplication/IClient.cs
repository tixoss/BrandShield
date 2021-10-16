using BrandShield.Common;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrandShield.ClientApplication
{
    internal interface IClient
    {
        Task<Guid> PostTranslationTasks(IEnumerable<TranslationTask> tasks);
        Task DeleteTranslationTasks(Guid executionId);
    }

    internal class FakeClient : IClient
    {
        public async Task<Guid> PostTranslationTasks(IEnumerable<TranslationTask> tasks)
        {
            Console.WriteLine($"Post {tasks.Count()} tasks");

            await Task.Delay(2000).ConfigureAwait(false);

            var retVal = Guid.NewGuid();

            Console.WriteLine($"Return executionId = {retVal}");
            return retVal;
        }

        public async Task DeleteTranslationTasks(Guid executionId)
        {
            Console.WriteLine($"Delete executionId = {executionId}");
            await Task.Delay(2000).ConfigureAwait(false);
        }
    }

    internal class Client : IClient
    {
        private const string POST = "Translation";
        private const string DELETE = "Translation";

        private readonly IClientConfig _config;


        public Client(IClientConfig config)
        {
            _config = config;
        }

        public async Task<Guid> PostTranslationTasks(IEnumerable<TranslationTask> tasks)
        {
            var serviceUrl = _config.BaseApiUrl.AppendPathSegment(POST);

            var response = await serviceUrl
                .PostJsonAsync(tasks)
                .ReceiveJson<Guid>()
                .ConfigureAwait(false);

            return response;
        }

        public async Task DeleteTranslationTasks(Guid executionId)
        {
            var serviceUrl = _config.BaseApiUrl
                .AppendPathSegment(DELETE)
                .AppendPathSegment(executionId);

            await serviceUrl
                .DeleteAsync()
                .ConfigureAwait(false);
        }
    }
}
