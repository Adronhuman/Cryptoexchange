using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace OrderBookMonitorBackend.Impl.Providers.Binance
{
    public class ApiClient
    {
        private readonly string _baseEndpoint = "https://api.binance.com/api/v3";
        private const string WebSocketUri = $"wss://stream.binance.com/stream?streams={"btceur"}@depth";
        private const int LIMIT = 100;
        private readonly IHttpClientFactory _httpClientFactory;

        private event EventHandler<Changes> UpdateStreamEvent = delegate { };

        public ApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            Task.Run(ReadStream);
        }

        public async Task<OrderBook?> GetOrderBook()
        {
            var endpoint = $"{_baseEndpoint}/depth?symbol=BTCEUR&limit={LIMIT}";

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

        public void PullUpdates(Action<Changes> onUpdate)
        {
            UpdateStreamEvent += (s, data) => onUpdate(data);
        }

        public async Task ReadStream()
        {
            // In the best case, the connection will be closed after 24 hours
            // However, we start fresh with each attempt in case of any crashes
            while (true)
            {
                Trace.TraceInformation("Initiating connection to the update stream: opening the WebSocket connection.");
                try
                {
                    await Connect();
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Websocket client crashed: {ex}");
                }
            }
        }

        public async Task Connect()
        {
            using var client = new ClientWebSocket();
            var cancellationToken = new CancellationToken();

            await client.ConnectAsync(new Uri(WebSocketUri), cancellationToken);

            // We might need to handle larger messages in the future,
            // but 20 KB is sufficient for now
            var buffer = new byte[1024 * 20];

            WebSocketReceiveResult? receiveResult = null;

            while (client.State == WebSocketState.Open
                && (receiveResult == null || !receiveResult.CloseStatus.HasValue))
            {
                try
                {
                    receiveResult = await client.ReceiveAsync(
                        new ArraySegment<byte>(buffer), cancellationToken);

                    var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);

                    var updateData = JsonSerializer.Deserialize<UpdateStreamEvent>(message, new JsonSerializerOptions
                    {
                        Converters = { new OrderConverter() }
                    });

                    if (updateData != null)
                    {
                        UpdateStreamEvent.Invoke(this, updateData.Data);
                    }
                    else
                    {
                        Trace.TraceWarning("Got empty message from binance stream, or couldn't deserialize data properly");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Update stream processing error: {ex}");
                }
            }
        }
    }
}
