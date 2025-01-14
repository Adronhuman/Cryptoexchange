using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace CryptoExchangeBackend.Providers.Binance
{
    public class ApiClient
    {
        private readonly string _baseEndpoint = "https://api.binance.com/api/v3";
        private const string WebSocketUri = $"wss://stream.binance.com/stream?streams={"btceur"}@depth";

        private readonly IHttpClientFactory _httpClientFactory;

        public ApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OrderBook> GetOrderBook()
        {
            var endpoint = $"{_baseEndpoint}/depth?symbol=BTCEUR&limit=5";

            var httpClient = _httpClientFactory.CreateClient();
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

        public async void PullUpdates(ChannelWriter<UpdateData> channel)
        {
            using var client = new ClientWebSocket();
            var cancellationToken = new CancellationToken();

            await client.ConnectAsync(new Uri(WebSocketUri), cancellationToken);

            // need to handle case when message is bigger
            var buffer = new byte[1024 * 20];

            WebSocketReceiveResult receiveResult = null;

            while (client.State == WebSocketState.Open
                && (receiveResult == null || !receiveResult.CloseStatus.HasValue))
            {
                try
                {
                    receiveResult = await client.ReceiveAsync(
                        new ArraySegment<byte>(buffer), cancellationToken);

                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    var updateData = JsonSerializer.Deserialize<DepthUpdateStreamEvent>(message, new JsonSerializerOptions
                    {
                        Converters = { new OrderConverter() }
                    });

                    Trace.TraceInformation("wrote to channel");
                    await channel.WriteAsync(updateData.Data);
                }
                catch { }
            }
        }
    }
}
