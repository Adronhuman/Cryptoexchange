using Core.Shared.Domain.Models;

namespace OrderBookMonitorBackend.Interfaces
{
    public interface IOrderBookProvider
    {
        Task<OrderBookSnapshot?> GetOrderBookSnapshot();
        void Subscribe(Action<OrderBookDiff, OrderBookSnapshot> updateHandler);
        Task RefreshAndListenChanges(CancellationToken token);
    }
}
