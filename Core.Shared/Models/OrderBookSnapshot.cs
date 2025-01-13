using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Shared.Models
{
    public class OrderBookSnapshot
    {
        public int TimeStamp { get; set; }
        public IEnumerable<Order> Bids { get; set; }
        public IEnumerable<Order> Asks { get; set; }
    }
}
