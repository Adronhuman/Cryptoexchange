using Core.Shared;
using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Interfaces;
using CryptoExchangeBackend.Providers;
using CryptoExchangeBackend.Providers.Binance;
using Microsoft.AspNetCore.SignalR;
using System.Data;
using System.Diagnostics;
using static Core.Shared.Constants;

namespace CryptoExchangeBackend.Workers
{
    public class BinanceWorker : BackgroundService
    {
        private readonly ApiClient _apiClient;
        private readonly IHubContext<OrderBookHub> _hubContext;
        private readonly MultiplePriceLevelsOrderBookProvider _mplorderBookProvider;
        private readonly List<CancelAndRestartTask> _tasks = [];

        public BinanceWorker(ApiClient httpClientFactory, IHubContext<OrderBookHub> hubContext, 
            MultiplePriceLevelsOrderBookProvider mplorderBookProvider)
        {
            this._apiClient = httpClientFactory;
            this._hubContext = hubContext;
            this._mplorderBookProvider = mplorderBookProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var levels = new[] { OrderBookSize.S5, OrderBookSize.S10, OrderBookSize.S20 };
            foreach (var level in levels)
            {
                var (provider, endpoint) = SetupOrderBookProviderForSize(level);
                _mplorderBookProvider.Configure(level, provider, endpoint);
            }

            // garbage collector please don't touch this stackframe 🥺
            await Task.Delay(Timeout.Infinite);
        }

        private (OrderBookProvider, string) SetupOrderBookProviderForSize(OrderBookSize size)
        {
            var orderBookProvider = new OrderBookProvider(_apiClient, size);
            var endpoint = string.Format(SignalREndpoints.OrderBookUpdate, (int)size);

            orderBookProvider.Subscribe((orderBookDiff, snapshot) =>
            {
                _hubContext.Clients.All.SendAsync(endpoint, orderBookDiff);
            });

            _tasks.Add(
                new CancelAndRestartTask((cancellationToken) =>
                {
                    var _ = orderBookProvider.RefreshAndListenChanges(cancellationToken);
                },
                TimeSpan.FromSeconds(10)
            ));

            return (orderBookProvider, endpoint);
        }
    }

    public class CancelAndRestartTask
    {
        private Timer _timer;
        private CancellationTokenSource _cancellation;

        public CancelAndRestartTask(Action<CancellationToken> fnc, TimeSpan period)
        {
            _cancellation = new CancellationTokenSource();
            _timer = new Timer((state) =>
            {
                try
                {
                    Trace.TraceInformation($"Timer is alive");
                    _cancellation.Cancel();
                    _cancellation = new CancellationTokenSource();
                    fnc(_cancellation.Token);
                    Trace.TraceInformation("Looks finished");
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Error in timer callback: {ex.Message}");
                }
            }, null, 0, 6000 * 1000);
        }
    }
}
