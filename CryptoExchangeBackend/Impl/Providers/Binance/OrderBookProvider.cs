using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using Core.Shared.Helpers;
using CryptoExchangeBackend.Interfaces;
using System.Diagnostics;
using System.Threading.Channels;
using static Core.Shared.Constants;
using DomainOrder = Core.Shared.Domain.Models.Order;
using DomainOrderBook = Core.Shared.Domain.Models.OrderBook;

namespace CryptoExchangeBackend.Impl.Providers.Binance
{
    public delegate void OrderBookUpdatedEventHandler(object sender, OrderBookDiff diff, OrderBookSnapshot snapshot);

    public class OrderBookProvider : IOrderBookProvider
    {
        private readonly int OrderBookSize;
        private readonly ApiClient _apiClient;
        private readonly OrderBookManager _orderBookManager;
        private readonly ChannelReader<Changes> _updateQueue;
        private OrderBookSnapshot LastSnapshot { get; set; }

        public event OrderBookUpdatedEventHandler Updated = delegate { };

        public OrderBookProvider(ApiClient apiClient, OrderBookSize size)
        {
            var channel = Channel.CreateUnbounded<Changes>();
            _apiClient = apiClient;
            _updateQueue = channel.Reader;
            _apiClient.PullUpdates(async (u) => await channel.Writer.WriteAsync(u));

            OrderBookSize = (int)size;
            _orderBookManager = new OrderBookManager(OrderBookSize);
        }

        public void Subscribe(Action<OrderBookDiff, OrderBookSnapshot> updateHandler)
        {
            Updated += (s, update, snapshot) => updateHandler(update, snapshot);
        }

        public Task<OrderBookSnapshot> GetOrderBookSnapshot()
        {
            return Task.FromResult(LastSnapshot);
        }

        public async Task RefreshAndListenChanges(CancellationToken cancellationToken)
        {
            var binanceModel = await _apiClient.GetOrderBook();
            var orderBook = Adapter.ToDomain(binanceModel);

            // Before moving to the new snapshot
            // We need to check the differences and notify clients
            if (LastSnapshot != null)
            {
                var mismatch = CalculateDifference(LastSnapshot.OrderBook, binanceModel);
                LastSnapshot = OrderBookSnapshot.Create(orderBook);
                Updated.Invoke(this, mismatch, LastSnapshot);
            }
            else
            {
                LastSnapshot = OrderBookSnapshot.Create(orderBook);
                Updated.Invoke(this, new OrderBookDiffBuilder().GetDiff(), LastSnapshot);
            }

            _orderBookManager.LoadInitial(orderBook.Bids, orderBook.Asks);

            var bidPrices = GetPricesSet(orderBook.Bids, OrderBookSize, desc: true);
            var askPrices = GetPricesSet(orderBook.Bids, OrderBookSize);

            var _ = Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var update = await _updateQueue.ReadAsync();
                        if (binanceModel.LastUpdateId > update.LastUpdateId)
                        {
                            continue;
                        }
                        var diffBuilder = new OrderBookDiffBuilder();
                        update.Bids.ForEach(bid => CollectChange(diffBuilder.BidChange, bidPrices, bid));
                        update.Asks.ForEach(ask => CollectChange(diffBuilder.AskChange, askPrices, ask));

                        // remove entries that exceed the current size
                        bidPrices.Skip(OrderBookSize)
                            .ToList()
                            .ForEach(price => CollectRemoval(diffBuilder.BidChange, bidPrices, price));
                        askPrices.Skip(OrderBookSize)
                            .ToList()
                            .ForEach(price => CollectRemoval(diffBuilder.AskChange, askPrices, price));

                        var diff = diffBuilder.GetDiff();

                        _orderBookManager.ApplyUpdate(diff);
                        var updatedBook = _orderBookManager.GetCurrentBook();
                        LastSnapshot = OrderBookSnapshot.Create(updatedBook);

                        Updated.Invoke(this, diff, LastSnapshot);
                    }
                    catch (Exception ex)
                    {
                        // Crashing the service is not worth a single missed message — simply log the error.
                        Trace.TraceError($"OrderBookProvider[{OrderBookSize}]: Failed to process update with {ex}");
                    }
                }
            }, CancellationToken.None);
        }

        // Think of this as a migration plan.
        // Clients need not only information about new or updated entries, 
        // but also instructions on how to handle the existing ones.
        private OrderBookDiff CalculateDifference(DomainOrderBook currentBook, OrderBook newBook)
        {
            var diffBuilder = new OrderBookDiffBuilder();

            var currentBidPrices = GetPricesSet(currentBook.Bids, OrderBookSize, desc: true);
            var currentAskPrices = GetPricesSet(currentBook.Asks, OrderBookSize);
            var newBidPrices = GetPricesSet(newBook.Bids, OrderBookSize, desc: true);
            var newAskPrices = GetPricesSet(newBook.Asks, OrderBookSize);

            // The logic for entries from the new order book remains the same as for stream updates
            newBook.Bids.ForEach(newBid => CollectChange(diffBuilder.BidChange, currentBidPrices, newBid));
            newBook.Asks.ForEach(newAsk => CollectChange(diffBuilder.AskChange, currentAskPrices, newAsk));

            // but we need to create a removal request for older entries if they are no longer present in the new book
            currentBidPrices
                .Where(price => !newBidPrices.Contains(price))
                .ToList().ForEach(price => CollectRemoval(diffBuilder.BidChange, currentBidPrices, price));

            currentAskPrices
                .Where(price => !newAskPrices.Contains(price))
                .ToList().ForEach(price => CollectRemoval(diffBuilder.AskChange, currentAskPrices, price));

            return diffBuilder.GetDiff();
        }

        private static void CollectRemoval(Action<decimal, decimal, ChangeType> apply, SortedSet<decimal> prices, decimal price)
        {
            prices.Remove(price);
            apply(price, 0, ChangeType.Deleted);
        }

        private static void CollectChange(Action<decimal, decimal, ChangeType> apply, SortedSet<decimal> prices, Order order)
        {
            var changeType = DetermineChange(prices, order);
            apply(order.Price, order.Quantity, changeType);
        }

        private static ChangeType DetermineChange(SortedSet<decimal> prices, Order order)
        {
            if (order.Quantity == 0)
            {
                prices.Remove(order.Price);
                return ChangeType.Deleted;
            }
            else if (prices.Contains(order.Price))
            {
                return ChangeType.Updated;
            }
            else
            {
                prices.Add(order.Price);
                return ChangeType.Added;
            }
        }

        private static SortedSet<decimal> GetPricesSet(IEnumerable<Order> orders, int size, bool desc = false)
        {
            var topN = orders.OrderBy(o => desc ? -o.Price : o.Price).Take(size).Select(o => o.Price);
            return new(topN, desc ? new DescendingComparer<decimal>() : null);
        }
        private static SortedSet<decimal> GetPricesSet(IEnumerable<DomainOrder> orders, int size, bool desc = false)
        {
            var topN = orders.OrderBy(o => desc ? -o.Price : o.Price).Take(size).Select(o => o.Price);
            return new(topN, desc ? new DescendingComparer<decimal>() : null);
        }

    }
}
