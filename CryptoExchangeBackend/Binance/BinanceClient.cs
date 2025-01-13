using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoExchangeBackend.Binance
{
    public class BinanceClient
    {
        private readonly string _baseEndpoint = "https://api.binance.com/api/v3";
        private const string WebSocketUri = $"wss://stream.binance.com/stream?streams={"btceur"}@depth";


        private readonly IHttpClientFactory httpClientFactory;

        public event EventHandler<DepthUpdate> OnDepthUpdate;

        public BinanceClient(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<OrderBook> GetOrderBook()
        {
            var endpoint = $"{_baseEndpoint}/depth?symbol=BTCEUR&limit=5";

            var httpClient = httpClientFactory.CreateClient();
            // TODO: add error handling
            var res = await httpClient.GetStringAsync(endpoint);

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new OrderConverter() }
            };
            var model = JsonSerializer.Deserialize<OrderBook>(res, serializeOptions);
            return model;
        }

        public async void ListenForUpdates()
        {
            using var client = new ClientWebSocket();
            var cancellationToken = new CancellationToken();

            await client.ConnectAsync(new Uri(WebSocketUri), cancellationToken);

            // need to handle case when message is bigger
            var buffer = new byte[1024 * 20];

            var receiveResult = await client.ReceiveAsync(
                new ArraySegment<byte>(buffer), cancellationToken);

            while (!receiveResult.CloseStatus.HasValue)
            {
                receiveResult = await client.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                var depthUpdate = JsonSerializer.Deserialize<DepthUpdateStreamEvent>(message, new JsonSerializerOptions
                {
                    Converters = { new OrderConverter() }
                });

                OnDepthUpdate.Invoke(this, depthUpdate?.Data);
            }
        }
    }
}
