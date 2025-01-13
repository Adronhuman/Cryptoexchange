using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Shared.Models
{
    public class Order(decimal price, decimal amount)
    {
        public decimal Price { get; set; } = price;
        public decimal Amount { get; set; } = amount;
    }
}
