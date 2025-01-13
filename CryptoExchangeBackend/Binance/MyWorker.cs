using System.Diagnostics;

namespace CryptoExchangeBackend.Binance
{
    public class MyWorker : BackgroundService
    {
        private readonly IServiceProvider sp;

        public MyWorker(IServiceProvider sp)
        {
            this.sp = sp;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = sp.CreateScope();
            var binanceClient = scope.ServiceProvider.GetService<BinanceClient>()!;

            binanceClient.OnDepthUpdate += (s, depthUpdate) =>
            {
                Trace.TraceInformation($"got depth update - {depthUpdate.LastUpdateId}");
            };
            binanceClient.ListenForUpdates();

            var orderBook = await binanceClient.GetOrderBook();

            while (true) { }
        }
    }
}
