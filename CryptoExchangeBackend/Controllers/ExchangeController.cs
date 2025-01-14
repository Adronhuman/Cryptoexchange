using Core.Shared.Domain.Models;
using CryptoExchangeBackend.Hubs;
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
        private readonly IOrderBookProvider _orderBookProvider;

        public ExchangeController(IHubContext<OrderBookHub> orderBookHub, IOrderBookProvider orderBookProvider)
        {
            this._hubContext = orderBookHub;
            this._orderBookProvider = orderBookProvider;
        }

        [HttpGet("orderBook")]
        public async Task<IActionResult> GetOrderBook()
        {
            var result = await _orderBookProvider.GetOrderBookSnapshot();
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
