using Core.Shared.ApiModels;
using Core.Shared.Domain.Models;
using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Impl.Providers;
using CryptoExchangeBackend.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace CryptoExchangeBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeController : ControllerBase
    {
        private readonly IHubContext<OrderBookHub> _hubContext;
        private readonly MultiplePriceLevelsOrderBookProvider _mplOrderBookProvider;

        public ExchangeController(IHubContext<OrderBookHub> orderBookHub, MultiplePriceLevelsOrderBookProvider orderBookProvider)
        {
            this._hubContext = orderBookHub;
            this._mplOrderBookProvider = orderBookProvider;
        }

        [HttpGet("orderBook")]
        public async Task<IActionResult> GetOrderBook(int size)
        {
            var (orderBookProvider, updateEndpoint) = _mplOrderBookProvider.DetermineOrderBookProvider(size);
            var snapshot = await orderBookProvider.GetOrderBookSnapshot();
            var result = new OrderBookResponse
            {
                Snapshot = snapshot,
                UpdateEndpoint = updateEndpoint
            };
            return Ok(result);
        }

        [HttpPost("testUpdate")]
        public async Task<IActionResult> PostUpdate(decimal price, decimal amount)
        {
            var orderBookDiff = new OrderBookDiff
            {
                Bids = [new OrderDiff { ChangeType = ChangeType.Added, Price = price, Amount = amount }],
                Asks = [],
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            var json = JsonSerializer.Serialize(orderBookDiff);
            await _hubContext.Clients.All.SendAsync("OrderBookUpdate", json);

            return Ok("posted");
        }
    }
}
