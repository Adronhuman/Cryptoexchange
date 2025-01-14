using Core.Shared.Domain.Models;

namespace CryptoExchangeBackend.Interfaces
{
    public interface IOrderBookProvider
    {
        Task<OrderBookSnapshot> GetOrderBookSnapshot();
        void Subscribe(Action<OrderBookDiff, OrderBookSnapshot> updateHandler);
        Task RefreshAndListenChanges(CancellationToken token);
    }
}
