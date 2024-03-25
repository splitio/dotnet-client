using Splitio.Services.Logger;
using Splitio.Services.Shared.Interfaces;
using System.Collections.Concurrent;

namespace Splitio.Services.Shared.Classes
{
    public class FactoryInstantiationsService : IFactoryInstantiationsService
    {
        private static FactoryInstantiationsService _instance;
        private static readonly object _instanceLock = new object();
        private static readonly object _lock = new object();
        private readonly ISplitLogger _log = WrapperAdapter.Instance().GetLogger(typeof(FactoryInstantiationsService));

        private readonly ConcurrentDictionary<string, int> _factoryInstantiations;

        public static IFactoryInstantiationsService Instance()
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new FactoryInstantiationsService();
                    }
                }
            }

            return _instance;
        }

        private FactoryInstantiationsService()
        {
            _factoryInstantiations = new ConcurrentDictionary<string, int>();
        }

        public void Decrease(string apiKey)
        {
            lock (_lock)
            {
                if (_factoryInstantiations.TryGetValue(apiKey, out int quantity))
                {
                    if (quantity == 1)
                    {
                        _factoryInstantiations.TryRemove(apiKey, out int value);

                        return;
                    }

                    var newQuantity = quantity - 1;

                    _factoryInstantiations.TryUpdate(apiKey, newQuantity, quantity);
                }
            }
        }

        public void Increase(string apiKey)
        {
            lock (_lock)
            {
                var exists = _factoryInstantiations.TryGetValue(apiKey, out int quantity);

                if (exists)
                {
                    if (quantity >= 1)
                    {
                        _log.Warn($"factory instantiation: You already have {quantity} factories with this API Key. We recommend keeping only one instance of the factory at all times(Singleton pattern) and reusing it throughout your application.");
                    }

                    var newQuantity = quantity + 1;

                    _factoryInstantiations.TryUpdate(apiKey, newQuantity, quantity);

                    return;
                }

                if (_factoryInstantiations.Count > 0)
                {
                    _log.Warn("factory instantiation: You already have an instance of the Split factory. Make sure you definitely want this additional instance. We recommend keeping only one instance of the factory at all times(Singleton pattern) and reusing it throughout your application.");
                }

                _factoryInstantiations.TryAdd(apiKey, 1);
            }
        }

        public int GetActiveFactories()
        {
            return _factoryInstantiations.Count;
        }

        public int GetRedundantActiveFactories()
        {
            var toReturn = 0;

            var keys = _factoryInstantiations.Keys;

            foreach (var key in keys)
            {
                var exists = _factoryInstantiations.TryGetValue(key, out int quantity);

                if (!exists) continue;

                if (quantity > 1)
                {
                    toReturn += quantity-1;
                }
            }

            return toReturn;
        }

        //This method is only for test
        public ConcurrentDictionary<string, int> GetInstantiations()
        {
            return _factoryInstantiations;
        }

        //This method is only for test
        public void Clear()
        {
            _factoryInstantiations.Clear();
        }
    }
}
