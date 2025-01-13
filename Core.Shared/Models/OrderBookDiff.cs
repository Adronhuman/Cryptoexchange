namespace Core.Shared.Models
{
    public class OrderBookDiff
    {
        public long TimeStamp { get; set; }
        public IEnumerable<OrderDiff> Bids { get; set; }
        public IEnumerable<OrderDiff> Asks { get; set; }
    }

    public class OrderDiff
    {
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public ChangeType ChangeType { get; set; }
    }

    public enum ChangeType
    {
        Updated,
        Added,
        Deleted
    }
}
