namespace Tests.Utils
{
    public static class RandomUtils
    {
        private static Random _random = new Random();
        public static decimal GetRandomDecimal()
        {
            return (decimal)(_random.NextDouble() * 100);
        }

        public static decimal GenerateUniqueDecimalNotInCollection(this IEnumerable<decimal> source)
        {
            decimal randomValue;
            do
            {
                randomValue = GetRandomDecimal();
            } while (source.Contains(randomValue));

            return randomValue;
        }

        public static (int, T) PickRandom<T>(this IEnumerable<T> source)
        {
            var list = source.ToList();
            var i = _random.Next(list.Count);

            return (i, list[i]);
        }

        public static IEnumerable<T> RandomlyPermutate<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(_ => _random.Next());
        }
    }
}
