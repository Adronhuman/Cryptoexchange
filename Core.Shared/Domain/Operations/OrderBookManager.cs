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


        public decimal CalculatePrice(decimal requestedAmount)
        {
            var topAsks = SelectTopOrders(Asks, Size);
            if (!topAsks.Any())
            {
                return 0;
            }

            decimal totalPrice = 0;
            decimal amountToCover = requestedAmount;
            foreach (var ask in topAsks)
            {
                if (amountToCover <= ask.Price)
                {
                    totalPrice += ask.Price * amountToCover;
                    amountToCover = 0;
                    break;
                }
                else
                {
                    amountToCover -= ask.Amount;
                    totalPrice += ask.Amount * ask.Price;
                }
            }

            if (amountToCover == 0) return totalPrice;

            // Warning: This is a simplistic solution that should be revisited in the future.
            // For now, the remaining amount is covered using the price of the last known ask.
            // Certainly some math model could be used to reflect actual market trend
            // - but this is out of scope for now
            var biggestPrice = topAsks.Last().Price;
            totalPrice += amountToCover * biggestPrice;
            return totalPrice;
        }

        private static IEnumerable<Order> SelectTopOrders(SortedDictionary<decimal, decimal> collection, int size)
        {
            return collection
                .Take(size)
                .Select((keyValue) => new Order(keyValue.Key, keyValue.Value));
        }

    }
}
