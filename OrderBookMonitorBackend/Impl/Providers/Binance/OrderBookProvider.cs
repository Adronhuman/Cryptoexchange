using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using OrderBookMonitorBackend.Interfaces;
using System.Diagnostics;
using System.Threading.Channels;
using static Core.Shared.Constants;

namespace OrderBookMonitorBackend.Impl.Providers.Binance
{
    public delegate void OrderBookUpdatedEventHandler(object sender, OrderBookDiff diff, OrderBookSnapshot snapshot);

    public class OrderBookProvider : IOrderBookProvider
    {
        private readonly int OrderBookSize;
        private readonly ApiClient _apiClient;
        private readonly OrderBookManager _orderBookManager;
        private readonly ChannelReader<Changes> _updateQueue;
        private OrderBookSnapshot? LastSnapshot { get; set; }

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

        public Task<OrderBookSnapshot?> GetOrderBookSnapshot()
        {
            return Task.FromResult(LastSnapshot);
        }

        public async Task RefreshAndListenChanges(CancellationToken cancellationToken)
        {
            var binanceModel = await _apiClient.GetOrderBook();
            if (binanceModel == null)
            {
                // nothing we can do here - wait for another RefreshAndListenChanges
                Trace.TraceWarning("Failed to get orderBookSnapshot from Binance REST endpoint");
                return;
            }

            var orderBook = Adapter.ToDomain(binanceModel);

            var changeManager = new ChangeManager<Order>(
                SelectPrice: o => o.Price,
                SelectAmount: o => o.Quantity,
                ClassifyChange: DetermineChange,
                bookSize: OrderBookSize
            );

            // Before moving to the new snapshot
            // We need to check the differences and notify clients
            if (LastSnapshot != null)
            {
                changeManager.Prepare(LastSnapshot.OrderBook.Bids, LastSnapshot.OrderBook.Asks);
                var mismatch = changeManager.CalculateDifference(binanceModel.Bids, binanceModel.Asks);
                LastSnapshot = OrderBookSnapshot.Create(orderBook);
                Updated.Invoke(this, mismatch, LastSnapshot);
            }
            else
            {
                LastSnapshot = OrderBookSnapshot.Create(orderBook);
                Updated.Invoke(this, new OrderBookDiffBuilder().GetDiff(), LastSnapshot);
            }

            changeManager.Prepare(binanceModel.Bids, binanceModel.Asks);
            _orderBookManager.LoadInitial(orderBook.Bids, orderBook.Asks);

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
                        var diff = changeManager.ProcessUpdate(update.Bids, update.Asks);
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

        // These rules are designed to be as decoupled as possible.
        // They are specific to the provider and the API at any given time.
        private static ChangeType DetermineChange(ISet<decimal> prices, Order order)
        {
            if (order.Quantity == 0)
            {
                return ChangeType.Deleted;
            }
            else if (prices.Contains(order.Price))
            {
                return ChangeType.Updated;
            }
            else
            {
                return ChangeType.Added;
            }
        }
    }
}
