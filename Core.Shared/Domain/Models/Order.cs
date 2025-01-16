namespace Core.Shared.Domain.Models
{
    public class Order(decimal price, decimal amount)
    {
        public decimal Price { get; set; } = price;
        public decimal Amount { get; set; } = amount;

        public override string ToString()
        {
            return $"{Price}_{Amount}";
        }
    }
}
