using Core.Shared.ApiModels;
using CryptoExchangeBackend.Hubs;
using CryptoExchangeBackend.Impl.Providers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace CryptoExchangeBackend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExchangeController : ControllerBase
    {
        private readonly MultiplePriceLevelsOrderBookProvider _mplOrderBookProvider;

        public ExchangeController(IHubContext<OrderBookHub> orderBookHub, MultiplePriceLevelsOrderBookProvider orderBookProvider)
        {
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
    }
}
