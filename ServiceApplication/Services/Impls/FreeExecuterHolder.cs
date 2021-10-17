using System.Collections.Generic;
using System.Linq;

namespace ServiceApplication.Services.Impls
{
    // TODO FSY: To real singleton
    internal class FreeExecuterHolder : IFreeExecuterHolder
    {
        private readonly int _maxExecuterCount;
        private readonly LinkedList<IExecuterConnection> _executerConnections;

        public FreeExecuterHolder(int maxExecuterCount) : this(maxExecuterCount, Enumerable.Empty<IExecuterConnection>()) { }

        public FreeExecuterHolder(int maxExecuterCount, IEnumerable<IExecuterConnection> executerConnections)
        {
            _maxExecuterCount = maxExecuterCount;
            _executerConnections = new LinkedList<IExecuterConnection>();

            foreach (var executerConnection in executerConnections)
            {
                _executerConnections.AddLast(executerConnection);
            }
        }

        public void Add(IExecuterConnection item)
        {
            lock (_executerConnections)
            {
                if (_executerConnections.Count < _maxExecuterCount)
                {
                    _executerConnections.AddLast(item);
                    return;
                }
            }

            item.Die();
        }

        public IExecuterConnection Get()
        {
            lock (_executerConnections)
            {
                if (_executerConnections.Any())
                {
                    var first = _executerConnections.First();
                    _executerConnections.RemoveFirst();
                    return first;
                }
            }

            return null;
        }
    }
}
