using Core.Shared.Models;

namespace CryptoExchangeFrontend
{
    public class OrderBookService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public event EventHandler OnOrderBookUpdated;

        public OrderBookService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OrderBookSnapshot> GetOrderBookAsync()
        {
            var httpClient = _httpClientFactory.CreateClient("OrderBookHttpClient");
            return await httpClient.GetFromJsonAsync<OrderBookSnapshot>("exchange/orderbook");
        }

        public async Task StartListening()
        {

        }
    }
}
