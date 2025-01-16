using Core.Shared.Domain.Models;
using Tests.Utils;

namespace Tests.Models
{
    public class OrderFromCryptoInc(decimal somePrice, decimal someAmount)
    {
        public decimal SomePrice { get; set; } = somePrice;
        public decimal SomeAmount { get; set; } = someAmount;

        public Order ToDomain()
        {
            return new Order(SomePrice, SomeAmount);
        }

        public OrderFromCryptoInc DeepCopy()
        {
            return new OrderFromCryptoInc(SomePrice, SomeAmount);
        }

        public override string ToString()
        {
            return $"{SomePrice}_{SomeAmount}";
        }

        public static ChangeType ClassifyChange(ISet<decimal> prices, OrderFromCryptoInc order)
        {
            if (order.SomePrice == -1)
            {
                return ChangeType.Deleted;
            }
            if (prices.Contains(order.SomePrice))
            {
                return ChangeType.Updated;
            }

            return ChangeType.Added;
        }

        public static OrderFromCryptoInc CreateRandom()
        {
            return new OrderFromCryptoInc(
                somePrice: RandomUtils.GetRandomDecimal(),
                someAmount: RandomUtils.GetRandomDecimal()
            );
        }
    }
}
