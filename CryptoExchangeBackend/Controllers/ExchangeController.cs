using Core.Shared.Models;
using CryptoExchangeBackend.Hubs;
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

        public ExchangeController(IHubContext<OrderBookHub> orderBookHub)
        {
            this._hubContext = orderBookHub;
        }

        [HttpGet("orderBook")]
        public IActionResult GetOrderBook()
        {
            var mockResult = new OrderBookSnapshot { 
                Bids = [new Order(10, 100)],
                Asks = [new Order(228, 1488)]
            };

            return Ok(mockResult);
        }

        [HttpPost("testUpdate")]
        public async Task<IActionResult> PostUpdate(decimal price, decimal amount)
        {
            var orderBookDiff = new OrderBookDiff 
            { 
                Bids = [new OrderDiff { ChangeType = ChangeType.Added, Price=price, Amount = amount}], 
                Asks = [], 
                TimeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            };
            var json = JsonSerializer.Serialize(orderBookDiff);
            await _hubContext.Clients.All.SendAsync("OrderBookUpdate", json);

            return Ok("posted");
        }
    }
}
