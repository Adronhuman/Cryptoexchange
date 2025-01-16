using Core.Shared.Domain.Models;

namespace CryptoExchangeBackend.Interfaces
{
    public interface IOrderBookLogger
    {
        void LogSnapshot(OrderBookSnapshot snapshot);
    }
}
