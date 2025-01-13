using BlazorAppWebAssembly.Client.Models;
using BlazorAppWebAssembly.Client.Settings;
using Core.Shared;
using Core.Shared.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;

namespace BlazorAppWebAssembly.Client.Services
{
    public class OrderBookService: IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly OrderBookApiInfo _apiInfo;
        private readonly HttpClient _httpClient;
        private readonly OrderBookManager _orderBookManager;

        public event EventHandler<OrderBookDiff> OrderBookUpdate = delegate { };
        public event EventHandler<OrderBookView> NewOrderBook = delegate { };

        public List<int> AvailableSizes 
        {
            get { return _apiInfo.DepthLevels; } 
        }

        public OrderBookService(HubConnection hubConnection, 
            IHttpClientFactory httpClientFactory,
            OrderBookApiInfo apiInfo)
        {
            this._hubConnection = hubConnection;
            this._apiInfo = apiInfo;
            this._httpClient = httpClientFactory.CreateClient("OrderBookHttpClient");
            _orderBookManager = new OrderBookManager(10);
        }

        // TODO: add error handling
        public async Task InitializeOrderBookAsync(int size)
        {
            var updates = new ConcurrentStack<OrderBookDiff>();

            EventHandler<OrderBookDiff> collectUpdate = (e, update) => updates.Push(update);
            SubscribeToUpdate(collectUpdate);

            await ListenToUpdates(size);

            var snapshot = await GetWholeOrderBook(size);
            _orderBookManager.LoadInitial(snapshot.Bids, snapshot.Asks);

            // Apply any updates from the stack that are newer than the snapshot
            while (updates.TryPop(out var update))
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
            _hubConnection.On<string>("OrderBookUpdate", (diffJson) =>
            {
                try
                {
                    var orderBookDiff = JsonSerializer.Deserialize<OrderBookDiff>(diffJson);
                    if (orderBookDiff != null) 
                        OrderBookUpdate.Invoke(this, orderBookDiff);
                }
                catch (Exception ex){
                
                }
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
