using BrandShield.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface IExecuterConnection
    {
        Task ExecuteAsync(IEnumerable<TranslationTask> translationTasks, CancellationToken ct);
        Task ExecuteAsync(TranslationTask translationTask, CancellationToken ct);
        Task DieAsync();
    }
}
