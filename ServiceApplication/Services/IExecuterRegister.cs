using BrandShield.Common;
using System;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public interface IExecuterRegister
    {
        void Place(Guid executionId, TranslationTask[] translationTasks);
        Task Break(Guid executionId);
    }
}
