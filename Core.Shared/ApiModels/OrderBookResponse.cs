using Core.Shared.Domain.Models;

namespace Core.Shared.ApiModels
{
    public class OrderBookResponse
    {
        public OrderBookSnapshot Snapshot { get; set; }
        public string UpdateEndpoint { get; set; }
    }
}
