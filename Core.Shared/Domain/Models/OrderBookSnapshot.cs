namespace Core.Shared.Domain.Models
{
    public class OrderBookSnapshot
    {
        public long TimeStamp { get; set; }
        public OrderBook OrderBook { get; set; }

        public static OrderBookSnapshot Create(OrderBook orderBook)
        {
            return new OrderBookSnapshot
            {
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                OrderBook = orderBook
            };
        }

        public static OrderBookSnapshot CreateDummy()
        {
            return new OrderBookSnapshot
            {
                // Any update should be considered more up-to-date than the absence of data
                TimeStamp = 0,
                OrderBook = new OrderBook { Bids = [], Asks = [] }
            };
        }
    }
}
