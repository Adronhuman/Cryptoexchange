using Microsoft.AspNetCore.SignalR;

namespace CryptoExchangeBackend.Hubs
{
    public class OrderBookHub : Hub
    {
        public async Task SendOrderBookUpdate(string orderBookDiff)
        {
            await Clients.All.SendAsync("OrderBookUpdate", orderBookDiff);
        }
    }
}
