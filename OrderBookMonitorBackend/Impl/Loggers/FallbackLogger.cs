using Core.Shared.Domain.Models;
using OrderBookMonitorBackend.Interfaces;

namespace OrderBookMonitorBackend.Impl.Loggers
{
    public class FallbackLogger : IOrderBookLogger
    {
        public void LogSnapshot(OrderBookSnapshot snapshot)
        {
            // pass
        }
    }
}
