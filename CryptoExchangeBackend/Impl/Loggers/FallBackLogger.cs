using Core.Shared.Domain.Models;
using CryptoExchangeBackend.Interfaces;

namespace CryptoExchangeBackend.Impl.Loggers
{
    public class FallbackLogger : IOrderBookLogger
    {
        public void LogSnapshot(OrderBookSnapshot snapshot)
        {
            // pass
        }
    }
}
