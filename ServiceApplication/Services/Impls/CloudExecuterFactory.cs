using BrandShield.Common;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    public class CloudExecuterFactory : ICloudExecuterFactory
    {
        private static readonly object _lock = new object();
        private static ICloudExecuterFactory _instance = null;
        private static readonly ValueTask<IExecuterConnection> _nullResult = new ValueTask<IExecuterConnection>((IExecuterConnection)null);

        private static int _countStartedMachines = 0;
        private static int _maxMachinesLimit;

        private CloudExecuterFactory(int maxMachinesLimit) {
            _maxMachinesLimit = maxMachinesLimit;
        }

        public static ICloudExecuterFactory GetInstance(int maxMachinesLimit)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = _instance ?? new CloudExecuterFactory(maxMachinesLimit);
                }
            }

            return _instance;
        }

        public ValueTask<IExecuterConnection> GetNewAsync()
        {
            if (_countStartedMachines < _maxMachinesLimit)
            {
                lock (_lock)
                {
                    if (_countStartedMachines < _maxMachinesLimit)
                    {
                        Interlocked.Increment(ref _countStartedMachines);
                        var creationTask = CreateNew();
                        return new ValueTask<IExecuterConnection>(creationTask);
                    }
                }
            }

            return _nullResult;
        }

        private async Task<IExecuterConnection> CreateNew()
        {
            // TODO FSY: Communication to cloud provider to create new instance of machine
            await Task.Delay(1000);
            return new FakeExecuterConnection(this);
        }

        private void DecrementCount()
        {
            Interlocked.Decrement(ref _countStartedMachines);
        }

        internal class FakeExecuterConnection : IExecuterConnection
        {
            private readonly CloudExecuterFactory _cloudExecuterFactory;

            public FakeExecuterConnection(CloudExecuterFactory cloudExecuterFactory)
            {
                _cloudExecuterFactory = cloudExecuterFactory;
            }

            public Task ExecuteAsync(TranslationTask translationTask)
            {
                throw new NotImplementedException();
            }

            public Task ExecuteAsync(IEnumerable<TranslationTask> translationTasks)
            {
                return Task.Delay(200);
            }

            public Task DieAsync()
            {
                _cloudExecuterFactory.DecrementCount();
                return Task.Delay(100);
            }
        }
    }
}
