using OrderBookMonitorBackend.Interfaces;
using static Core.Shared.Constants;

namespace OrderBookMonitorBackend.Impl.Providers
{
    public class MultiplePriceLevelsOrderBookProvider
    {
        // Contains orderBookProvider for certain size, and endpoint for listening to updates
        private readonly Dictionary<int, (IOrderBookProvider, string)> _orderBookProviders;

        public MultiplePriceLevelsOrderBookProvider()
        {
            _orderBookProviders = [];
        }

        public void Configure(OrderBookSize size, IOrderBookProvider orderBookProvider, string updateEndpoint)
        {
            _orderBookProviders.Add((int)size, (orderBookProvider, updateEndpoint));
        }

        public (IOrderBookProvider, string) DetermineOrderBookProvider(int size)
        {
            var levels = _orderBookProviders.Keys.Order();
            foreach (var level in levels)
            {
                if (level >= size) return _orderBookProviders[level];
            }
            return _orderBookProviders[levels.Last()];
        }
    }
}
