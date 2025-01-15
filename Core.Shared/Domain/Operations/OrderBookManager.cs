using Core.Shared.Domain.Models;
using Core.Shared.Helpers;

namespace Core.Shared.Domain.Operations
{
    public class OrderBookManager
    {
        private SortedDictionary<decimal, decimal> Bids { get; set; }
        private SortedDictionary<decimal, decimal> Asks { get; set; }
        private int Size { get; set; }

        public OrderBookManager(int size)
        {
            Bids = new(new DescendingComparer<decimal>());
            Asks = [];
            Size = size;
        }

        public void LoadInitial(IEnumerable<Order> bids, IEnumerable<Order> asks)
        {
            Bids.Clear();
            Asks.Clear();
            foreach (var bid in bids)
            {
                Bids[bid.Price] = bid.Amount;
            }

            foreach (var ask in asks)
            {
                Asks[ask.Price] = ask.Amount;
            }
        }

        public void ApplyUpdate(OrderBookDiff diff)
        {
            UpdateOrders(Bids, diff.Bids);
            UpdateOrders(Asks, diff.Asks);
        }

        private void UpdateOrders(SortedDictionary<decimal, decimal> orders, IEnumerable<OrderDiff> diffs)
        {
            foreach (var orderChange in diffs)
            {
                switch (orderChange.ChangeType)
                {
                    case ChangeType.Added:
                    case ChangeType.Updated:
                        {
                            orders[orderChange.Price] = orderChange.Amount;
                            break;
                        }
                    case ChangeType.Deleted:
                        {
                            orders.Remove(orderChange.Price);
                            break;
                        }
                }
            }
        }

        public OrderBook GetCurrentBook()
        {
            var view = new OrderBook
            {
                Bids = SelectTopOrders(Bids, Size),
                Asks = SelectTopOrders(Asks, Size)
            };

            return view;
        }

        private static IEnumerable<Order> SelectTopOrders(SortedDictionary<decimal, decimal> collection, int size)
        {
            return collection
                .Take(size)
                .Select((keyValue) => new Order(keyValue.Key, keyValue.Value));
        }

    }
}
