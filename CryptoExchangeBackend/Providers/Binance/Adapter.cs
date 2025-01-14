using Core.Shared.Domain.Models;

namespace CryptoExchangeBackend.Providers.Binance
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

    public class OrderBookDiffBuilder
    {
        private List<OrderDiff> BidChanges { get; set; }
        private List<OrderDiff> AskChanges { get; set; }
        public OrderBookDiffBuilder()
        {
            BidChanges = [];
            AskChanges = [];
        }

        public void BidChange(decimal price, decimal amount, ChangeType type)
        {
            BidChanges.Add(new OrderDiff { Price = price, Amount = amount, ChangeType = type });
        }

        public void AskChange(decimal price, decimal amount, ChangeType type)
        {
            AskChanges.Add(new OrderDiff { Price = price, Amount = amount, ChangeType = type });
        }

        public OrderBookDiff GetDiff()
        {
            return new OrderBookDiff
            {
                Bids = BidChanges,
                Asks = AskChanges,
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
        }

        //public static Core.Shared.Models.OrderBookDiff CreateDomainOrderDiff(DepthUpdateStreamEvent updateEvent)
        //{
        //    var diff = new OrderBookDiff();
        //    diff.TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();


        //}
    }
}
