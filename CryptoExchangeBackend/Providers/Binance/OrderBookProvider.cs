using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using CryptoExchangeBackend.Interfaces;
using System.Diagnostics;
using System.Threading.Channels;

namespace CryptoExchangeBackend.Providers.Binance
{
    public delegate void OrderBookUpdatedEventHandler(object sender, OrderBookDiff diff, OrderBookSnapshot snapshot);

    public class OrderBookProvider : IOrderBookProvider
    {
        private readonly ApiClient _apiClient;
        private readonly OrderBookManager _orderBookManager;

        private readonly ChannelReader<UpdateData> _updateQueue;

        private OrderBookSnapshot CurrentOrderBook { get; set; }

        public event OrderBookUpdatedEventHandler Updated = delegate { };

        public OrderBookProvider(IHttpClientFactory httpClientFactory)
        {
            var channel = Channel.CreateUnbounded<UpdateData>();
            _apiClient = new ApiClient(httpClientFactory);
            _updateQueue = channel.Reader;
            _apiClient.PullUpdates(channel.Writer);

            _orderBookManager = new OrderBookManager(20);
        }

        public Task<OrderBookSnapshot> GetOrderBookSnapshot()
        {
            return Task.FromResult(CurrentOrderBook);
        }

        public void Subscribe(Action<OrderBookDiff, OrderBookSnapshot> updateHandler)
        {
            Updated += (s, update, snapshot) => updateHandler(update, snapshot);
        }

        public async Task RefreshAndListenChanges(CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RefreshAndListenChanges fired");
            var orderBook = Adapter.ToDomain(await _apiClient.GetOrderBook());
            _orderBookManager.LoadInitial(orderBook.Bids, orderBook.Asks);
            CurrentOrderBook = OrderBookSnapshot.Create(orderBook);

            var bids = new HashSet<decimal>(orderBook.Bids.Select(o => o.Price));
            var asks = new HashSet<decimal>(orderBook.Asks.Select(o => o.Price));

            await Task.Run(async () =>
            {
                while (true)
                {
                    var update = await _updateQueue.ReadAsync();
                    Trace.TraceInformation("read from channel");
                    var diffBuilder = new OrderBookDiffBuilder();
                    foreach (var bid in update.Bids)
                    {
                        var changeType = CalculateChange(bids, bid);
                        diffBuilder.BidChange(bid.Price, bid.Quantity, changeType);
                    }
                    foreach (var ask in update.Asks)
                    {
                        var changeType = CalculateChange(asks, ask);
                        diffBuilder.AskChange(ask.Price, ask.Quantity, changeType);
                    }
                    var diff = diffBuilder.GetDiff();

                    _orderBookManager.ApplyUpdate(diff);
                    var updatedBook = _orderBookManager.GetCurrentBook();
                    CurrentOrderBook = OrderBookSnapshot.Create(updatedBook);

                    Updated.Invoke(this, diff, CurrentOrderBook);
                }
            }, cancellationToken);
        }

        private static ChangeType CalculateChange(HashSet<decimal> prices, Order order)
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
    }
}
