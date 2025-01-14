using Core.Shared.Domain.Models;

namespace Frontend.Client.Models
{
    public class OrderBookView
    {
        public int Depth { get; set; }
        public IEnumerable<Order> Bids { get; set; }
        public IEnumerable<Order> Asks { get; set; }
    }
}
