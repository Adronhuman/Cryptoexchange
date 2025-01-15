using Core.Shared.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Shared.ApiModels
{
    public class OrderBookResponse
    {
        public OrderBookSnapshot Snapshot { get; set; }
        public string UpdateEndpoint    { get; set; }
    }
}
