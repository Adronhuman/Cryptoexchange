using Core.Shared.Domain.Models;

namespace Core.Shared.Domain.Operations
{
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
    }
}
