using Core.Shared;
using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Web;

namespace Frontend.Client.Services
{
    public class OrderBookService : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly OrderBookApiInfo _apiInfo;
        private readonly HttpClient _httpClient;
        private readonly OrderBookManager _orderBookManager;

        public event EventHandler<OrderBookDiff> OrderBookUpdate = delegate { };
        public event EventHandler<OrderBook> NewOrderBook = delegate { };

        public List<int> AvailableSizes
        {
            get { return _apiInfo.DepthLevels; }
        }

        public OrderBookService(HubConnection hubConnection,
            IHttpClientFactory httpClientFactory,
            OrderBookApiInfo apiInfo)
        {
            _hubConnection = hubConnection;
            _apiInfo = apiInfo;
            _httpClient = httpClientFactory.CreateClient("OrderBookHttpClient");
            _orderBookManager = new OrderBookManager(20);
        }

        // TODO: add error handling
        public async Task InitializeOrderBookAsync(int size)
        {
            var updates = new Queue<OrderBookDiff>();
            EventHandler<OrderBookDiff> collectUpdate = (e, update) => updates.Enqueue(update);
            SubscribeToUpdate(collectUpdate);

            await ListenToUpdates(size);

            var snapshot = await GetWholeOrderBook(size);
            _orderBookManager.LoadInitial(snapshot.OrderBook.Bids, snapshot.OrderBook.Asks);

            // Apply any updates from the queue that are newer than the snapshot
            while (updates.TryDequeue(out var update))
            {
                if (update.TimeStamp >= snapshot.TimeStamp)
                    _orderBookManager.ApplyUpdate(update);
            }

            UnsubscribeFromUpdates(collectUpdate);

            // Handle all subsequent updates
            SubscribeToUpdate((s, update) =>
            {
                _orderBookManager.ApplyUpdate(update);
                var newOrderBook = _orderBookManager.GetCurrentBook();
                NewOrderBook.Invoke(this, newOrderBook);
            });

            NewOrderBook.Invoke(this, _orderBookManager.GetCurrentBook());
        }

        public async Task ListenToUpdates(int size)
        {
            _hubConnection.On<OrderBookDiff>(SignalREndpoints.OrderBookUpdate, (diff) =>
            {
                OrderBookUpdate.Invoke(this, diff);
            });

            await _hubConnection.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task<OrderBookSnapshot> GetWholeOrderBook(int size)
        {
            var endpoint = _apiInfo.WholeBookEndpoint;
            var queryArgs = HttpUtility.ParseQueryString(string.Empty);
            queryArgs["size"] = size.ToString();

            var snapshot = await _httpClient.GetFromJsonAsync<OrderBookSnapshot>($"{endpoint}?{queryArgs}");
            return snapshot;
        }

        private void SubscribeToUpdate(EventHandler<OrderBookDiff> handler)
        {
            OrderBookUpdate += handler;
        }

        private void UnsubscribeFromUpdates(EventHandler<OrderBookDiff> handler)
        {
            OrderBookUpdate -= handler;
        }

    }
}
