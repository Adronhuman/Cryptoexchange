using Core.Shared.Domain.Models;
using Core.Shared.Helpers;
using DomainOrder = Core.Shared.Domain.Models.Order;

namespace Core.Shared.Domain.Operations
{
    // T - provider-specific order model
    public class ChangeManager<TOrder>(Func<TOrder, decimal> SelectPrice,
        Func<TOrder, decimal> SelectAmount,
        Func<ISet<decimal>, TOrder, ChangeType> ClassifyChange,
        int bookSize)
    {
        private SortedSet<decimal> _bidPrices = [];
        private SortedSet<decimal> _askPrices = [];

        public void Prepare(IEnumerable<DomainOrder> bids, IEnumerable<DomainOrder> asks)
        {
            _bidPrices = GetPricesSet(bids, desc: true);
            _askPrices = GetPricesSet(asks);
        }

        public void Prepare(IEnumerable<TOrder> bids, IEnumerable<TOrder> asks)
        {
            _bidPrices = GetPricesSet(bids, desc: true);
            _askPrices = GetPricesSet(asks);
        }

        public OrderBookDiff ProcessUpdate(IEnumerable<TOrder> bids, IEnumerable<TOrder> asks)
        {
            var diffBuilder = new OrderBookDiffBuilder();

            bids.ToList().ForEach(bid => CollectChange(diffBuilder.BidChange, _bidPrices, bid));
            asks.ToList().ForEach(ask => CollectChange(diffBuilder.AskChange, _askPrices, ask));

            // remove entries that exceed the current size
            _bidPrices.Skip(bookSize)
                .ToList()
                .ForEach(price => CollectRemoval(diffBuilder.BidChange, _bidPrices, price));
            _askPrices.Skip(bookSize)
                .ToList()
                .ForEach(price => CollectRemoval(diffBuilder.AskChange, _askPrices, price));

            return diffBuilder.GetDiff();
        }

        // Think of this as a migration plan.
        // Clients need not only information about new or updated entries, 
        // but also instructions on how to handle the existing ones.
        public OrderBookDiff CalculateDifference(IEnumerable<TOrder> newBids, IEnumerable<TOrder> newAsks)
        {
            var diffBuilder = new OrderBookDiffBuilder();

            var newBidPrices = GetPricesSet(newBids, desc: true);
            var newAskPrices = GetPricesSet(newAsks);

            // The logic for entries from the new order book remains the same as for stream updates
            newBids.ToList().ForEach(newBid => CollectChange(diffBuilder.BidChange, _bidPrices, newBid));
            newAsks.ToList().ForEach(newAsk => CollectChange(diffBuilder.AskChange, _askPrices, newAsk));

            // but we need to create a removal request for older entries if they are no longer present in the new book
            _bidPrices
                .Where(price => !newBidPrices.Contains(price))
                .ToList().ForEach(price => CollectRemoval(diffBuilder.BidChange, _bidPrices, price));

            _askPrices
                .Where(price => !newAskPrices.Contains(price))
                .ToList().ForEach(price => CollectRemoval(diffBuilder.AskChange, _askPrices, price));

            return diffBuilder.GetDiff();
        }

        private void CollectRemoval(Action<decimal, decimal, ChangeType> apply, SortedSet<decimal> prices, decimal price)
        {
            prices.Remove(price);
            apply(price, 0, ChangeType.Deleted);
        }

        private void CollectChange(Action<decimal, decimal, ChangeType> apply, SortedSet<decimal> prices, TOrder order)
        {
            var changeType = ClassifyChange(prices, order);
            var price = SelectPrice(order);
            switch (changeType)
            {
                case ChangeType.Deleted:
                    prices.Remove(price);
                    break;
                case ChangeType.Added:
                    prices.Add(price);
                    break;
            }
            apply(SelectPrice(order), SelectAmount(order), changeType);
        }

        private SortedSet<decimal> GetPricesSet(IEnumerable<TOrder> orders, bool desc = false)
        {
            var topN = orders
                .Select(SelectPrice)
                .OrderBy(price => desc ? -price : price)
                .Take(bookSize);
            return new(topN, desc ? new DescendingComparer<decimal>() : null);
        }
        private SortedSet<decimal> GetPricesSet(IEnumerable<DomainOrder> orders, bool desc = false)
        {
            var topN = orders
                .Select(o => o.Price)
                .OrderBy(price => desc ? -price : price)
                .Take(bookSize);
            return new(topN, desc ? new DescendingComparer<decimal>() : null);
        }
    }
}
