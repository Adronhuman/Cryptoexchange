namespace Core.Shared.Domain.Models
{
    public class OrderBook
    {
        public IEnumerable<Order> Bids { get; set; }
        public IEnumerable<Order> Asks { get; set; }
    }
}
