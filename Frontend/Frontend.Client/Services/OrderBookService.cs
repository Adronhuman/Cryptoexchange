using Core.Shared.ApiModels;
using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using Frontend.Client.Settings;
using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System.Web;

namespace Frontend.Client.Services
{
    public class OrderBookService : IAsyncDisposable
    {
        private readonly HubConnection _hubConnection;
        private readonly OrderBookApiInfo _apiInfo;
        private readonly HttpClient _httpClient;
        private OrderBookManager _orderBookManager;
        private List<string> _activeHubMethods;
        private long CurrentOrderBookTimeStamp;
        private readonly Channel<OrderBookDiff> _updateChannel;
        private CancellationTokenSource _subscribeCancellation;
        
        public event EventHandler<OrderBookDiff>? OrderBookUpdate;
        public event EventHandler<OrderBook> NewOrderBook = delegate { };

        public OrderBookService(HubConnection hubConnection,
            IHttpClientFactory httpClientFactory,
            OrderBookApiInfo apiInfo)
        {
            _hubConnection = hubConnection;
            _activeHubMethods = [];
            _apiInfo = apiInfo;
            _httpClient = httpClientFactory.CreateClient("OrderBookHttpClient");
            _orderBookManager = new OrderBookManager(100);
            _updateChannel = Channel.CreateUnbounded<OrderBookDiff>();
        }

        // TODO: add error handling
        public async Task SetupOrderBookAsync(int size)
        {
            Refresh();
            var res = await GetOrderBookSnapshot(size);
            var snapshot = res.Snapshot;
            var endpointForUpdates = res.UpdateEndpoint;

            CurrentOrderBookTimeStamp = snapshot.TimeStamp;
            await ListenToUpdates(endpointForUpdates);
            
            _orderBookManager = new OrderBookManager(size);
            _orderBookManager.LoadInitial(snapshot.OrderBook.Bids, snapshot.OrderBook.Asks);

            SubscribeToUpdate((s, update) =>
            {
                if (update.TimeStamp <= CurrentOrderBookTimeStamp) return;

                _orderBookManager.ApplyUpdate(update);
                var newOrderBook = _orderBookManager.GetCurrentBook();
                NewOrderBook.Invoke(this, newOrderBook);
            });

            NewOrderBook.Invoke(this, _orderBookManager.GetCurrentBook());
        }

        public async Task ListenToUpdates(string endpoint)
        {
            _hubConnection.On<OrderBookDiff>(endpoint, (diff) =>
            {
                _updateChannel.Writer.TryWrite(diff);
            });
            _activeHubMethods.Add(endpoint);

            if (_hubConnection.State != HubConnectionState.Connected)
            {
                await _hubConnection.StartAsync();
            }
        }

        public decimal CalculatePrice(decimal requestedAmount)
        {
            var topAsks = _orderBookManager.GetCurrentBook().Asks;

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

            // Warning: a little bit of voodoo dance;
            // In the long run trade-off should be reconsidered
            // for now - cover the remaining amount using price from last known ask
            var biggestPrice = topAsks.Last().Price;
            totalPrice += amountToCover * biggestPrice;
            return totalPrice;
        }

        private void Refresh()
        {
            _subscribeCancellation?.Cancel();
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                _activeHubMethods.ForEach(_hubConnection.Remove);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection.State != HubConnectionState.Disconnected)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        private async Task<OrderBookResponse> GetOrderBookSnapshot(int size)
        {
            var endpoint = _apiInfo.WholeBookEndpoint;
            var queryArgs = HttpUtility.ParseQueryString(string.Empty);
            queryArgs["size"] = size.ToString();

            var res = await _httpClient.GetFromJsonAsync<OrderBookResponse>($"{endpoint}?{queryArgs}");
            return res;
        }

        private void SubscribeToUpdate(EventHandler<OrderBookDiff> handler)
        {
            OrderBookUpdate = handler;

            _subscribeCancellation = new CancellationTokenSource();
            _subscribeCancellation.Token.Register(() =>
            {
                OrderBookUpdate = null;
            });

            Task.Run(async () =>
            {
                while (!_subscribeCancellation.Token.IsCancellationRequested)
                {
                    var update = await _updateChannel.Reader.ReadAsync(_subscribeCancellation.Token);
                    if (update != null)
                    {
                        OrderBookUpdate?.Invoke(this, update);
                    }
                }
            });
        }
    }
}
