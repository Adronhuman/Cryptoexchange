using Core.Shared.Domain.Models;

namespace CryptoExchangeBackend.Impl.Providers.Binance
{
    public static class Adapter
    {
        public static OrderBookSnapshot CreateOrderBookSnapshot(OrderBook orderBook)
        {
            var snapshot = new OrderBookSnapshot
            {
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                OrderBook = ToDomain(orderBook)
            };

            return snapshot;
        }

        public static Core.Shared.Domain.Models.OrderBook ToDomain(OrderBook orderBook)
        {
            return new Core.Shared.Domain.Models.OrderBook
            {
                Bids = orderBook.Bids.Select(ToDomain),
                Asks = orderBook.Asks.Select(ToDomain)
            };
        }

        public static Core.Shared.Domain.Models.Order ToDomain(Order order)
        {
            return new Core.Shared.Domain.Models.Order(order.Price, order.Quantity);
        }
    }
}
