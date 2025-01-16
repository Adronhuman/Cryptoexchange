using Core.Shared.Domain.Models;

namespace OrderBookMonitorBackend.Interfaces
{
    public interface IOrderBookLogger
    {
        void LogSnapshot(OrderBookSnapshot snapshot);
    }
}
