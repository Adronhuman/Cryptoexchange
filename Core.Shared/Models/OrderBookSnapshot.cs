namespace Core.Shared.Models
{
    public class OrderBookSnapshot
    {
        public int TimeStamp { get; set; }
        public IEnumerable<Order> Bids { get; set; }
        public IEnumerable<Order> Asks { get; set; }
    }
}
