using Core.Shared;
using CryptoExchangeBackend.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Drawing;
using System.Net.Http;
using static Core.Shared.Constants;

namespace CryptoExchangeBackend.Providers.Binance
{
    public static class RegisterWorkers
    {
        public static void RegisterBinanceWorkers(this WebApplication app) 
        {
            var sp = app.Services;
            EnsureServicesRegistered(sp,
                typeof(ApiClient), 
                typeof(IHubContext<OrderBookHub>), 
                typeof(MultiplePriceLevelsOrderBookProvider));

            var apiClient = sp.GetService<ApiClient>()!;
            var hubContext = sp.GetService<IHubContext<OrderBookHub>>()!;
            var mplOrderBookProvide = sp.GetService<MultiplePriceLevelsOrderBookProvider>()!;
            
            var levels = new[] { OrderBookSize.S5, OrderBookSize.S10, OrderBookSize.S20 };
            foreach (var level in levels)
            {
                var (provider, endpoint) = SetupOrderBookProviderForSize(level);
                mplOrderBookProvide.Configure(level, provider, endpoint);
            }

            (OrderBookProvider, string) SetupOrderBookProviderForSize(OrderBookSize size)
            {
                var orderBookProvider = new OrderBookProvider(apiClient, size);
                var endpoint = string.Format(SignalREndpoints.OrderBookUpdate, (int)size);

                orderBookProvider.Subscribe((orderBookDiff, snapshot) =>
                {
                    hubContext.Clients.All.SendAsync(endpoint, orderBookDiff);
                });

                var period = TimeSpan.FromMinutes(1);
                var cancellation = new CancellationTokenSource();
                var timer = new Timer((_) =>
                {
                    cancellation.Cancel();
                    cancellation = new CancellationTokenSource();
                    _ = orderBookProvider.RefreshAndListenChanges(cancellation.Token);
                }, null, 0, period.Milliseconds);

                return (orderBookProvider, endpoint);
            }
        }
        public static void EnsureServicesRegistered(this IServiceProvider serviceProvider, params Type[] serviceTypes)
        {
            foreach (var serviceType in serviceTypes)
            {
                var service = serviceProvider.GetService(serviceType)
                    ?? throw new InvalidOperationException($"{serviceType.Name} is not registered.");
            }
        }
    }
}
