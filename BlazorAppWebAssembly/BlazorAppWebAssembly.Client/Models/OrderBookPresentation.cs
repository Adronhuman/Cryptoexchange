using Core.Shared.Models;

namespace BlazorAppWebAssembly.Client.Models
{
    public class OrderBookView
    {
        public int Depth { get; set; }
        public IEnumerable<Order> Bids { get; set; }
        public IEnumerable<Order> Asks { get; set; }
    }
}
