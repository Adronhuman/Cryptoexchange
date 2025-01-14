using Core.Shared;
using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace CryptoExchangeBackend.Workers
{
    public class StreamingDataWorker : BackgroundService
    {
        private readonly IHubContext<OrderBookHub> _hubContext;
        private readonly IOrderBookProvider _orderBookProvider;

        public StreamingDataWorker(IHubContext<OrderBookHub> hubContext, IOrderBookProvider orderBookProvider)
        {
            this._hubContext = hubContext;
            this._orderBookProvider = orderBookProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _orderBookProvider.Subscribe((orderBookDiff, snapshot) =>
            {
                _hubContext.Clients.All.SendAsync(SignalREndpoints.OrderBookUpdate, orderBookDiff);
            });

            // Fetch the entire order book state every minute and apply changes in a new cycle based on the latest snapshot.
            var period = TimeSpan.FromMinutes(1);
            var cancellation = new CancellationTokenSource();
            var timer = new Timer((_) =>
            {
                cancellation.Cancel();
                cancellation = new CancellationTokenSource();
                _orderBookProvider.RefreshAndListenChanges(cancellation.Token);
            }, null, 0, period.Milliseconds);
        }
    }
}
