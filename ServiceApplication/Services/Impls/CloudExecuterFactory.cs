using BrandShield.Common;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceApplication.Services
{
    internal class CloudExecuterFactory : ICloudExecuterFactory
    {
        private const int CREATE_DELAY = 1000;
        private static readonly object _lock = new object();
        private static ICloudExecuterFactory _instance = null;
        private static readonly ValueTask<IExecuterConnection> _nullResult = new ValueTask<IExecuterConnection>((IExecuterConnection)null);

        private static int _countStartedMachines = 0;
        private static int _maxMachinesLimit;
        private readonly ILog _logger;

        private CloudExecuterFactory(int maxMachinesLimit, ILog logger) {
            _maxMachinesLimit = maxMachinesLimit;
            _logger = logger;
        }

        public static ICloudExecuterFactory GetInstance(int maxMachinesLimit, ILog logger)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance = _instance ?? new CloudExecuterFactory(maxMachinesLimit, logger);
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
                        _logger.InfoFormat("Create new machine instance. Count: {0}", _countStartedMachines);
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
            await Task.Delay(CREATE_DELAY).ConfigureAwait(false);
            return new FakeExecuterConnection(this);
        }

        private void DecrementCount()
        {
            Interlocked.Decrement(ref _countStartedMachines);
            _logger.InfoFormat("Shutdown machine instance. Count: {0}", _countStartedMachines);
        }

        internal class FakeExecuterConnection : IExecuterConnection
        {
            private const int PROCESSING_DELAY = 150;
            private const int DIE_DELAY = 100;
            private readonly CloudExecuterFactory _cloudExecuterFactory;

            public FakeExecuterConnection(CloudExecuterFactory cloudExecuterFactory)
            {
                _cloudExecuterFactory = cloudExecuterFactory;
            }

            public Task ExecuteAsync(TranslationTask translationTask, CancellationToken ct)
            {
                return Task.Delay(PROCESSING_DELAY, ct);
            }

            public Task ExecuteAsync(IEnumerable<TranslationTask> translationTasks, CancellationToken ct)
            {
                return Task.Delay(PROCESSING_DELAY * translationTasks.Count(), ct);
            }

            public Task DieAsync()
            {
                return Task.Delay(DIE_DELAY)
                    .ContinueWith(_ => {
                        _cloudExecuterFactory.DecrementCount();
                    });
            }
        }
    }
}
