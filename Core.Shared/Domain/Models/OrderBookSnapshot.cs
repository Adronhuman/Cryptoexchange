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
    }
}
