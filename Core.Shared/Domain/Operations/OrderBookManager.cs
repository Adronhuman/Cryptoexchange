using Core.Shared.Domain.Models;

namespace Core.Shared.Domain.Operations
{
    public class OrderBookManager
    {
        private SortedDictionary<decimal, decimal> Bids { get; set; }
        private SortedDictionary<decimal, decimal> Asks { get; set; }
        private int Size { get; set; }



        // to remove
        private HashSet<decimal> bids;
        private HashSet<decimal> asks;

        public OrderBookManager(int size)
        {
            Bids = new(new DescendingComparer<decimal>());
            Asks = [];
            Size = size;
        }

        public void LoadInitial(IEnumerable<Order> bids, IEnumerable<Order> asks)
        {
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

        public void ApplyUpdate(OrderBookDiff diff, HashSet<decimal> bids, HashSet<decimal> asks)
        {
            this.bids = bids;
            this.asks = asks;
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
                        {
                            if (!orders.TryAdd(orderChange.Price, orderChange.Amount))
                                //orders.Add(orderChange.Price, orderChange.Amount)
                                orders[orderChange.Price] = orderChange.Amount;
                            break;
                        }
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

    class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
    {
        public int Compare(T x, T y)
        {
            return y.CompareTo(x);
        }
    }
}
