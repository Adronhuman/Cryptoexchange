namespace Tests.Utils
{
    public static class CollectionUtils
    {
        public static IEnumerable<T> GenerateNUnique<T>(int size, Func<T> generator, Func<T, decimal> propertySelector)
        {
            var values = new HashSet<decimal>();
            var collection = new List<T>(size);
            do
            {
                var entry = generator();
                if (!values.Contains(propertySelector(entry)))
                {
                    collection.Add(entry);
                }
            }
            while (collection.Count < size);

            return collection;
        }

        public static IEnumerable<string> GetDifferences<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
        {
            var diff1 = collection1.Except(collection2).Select(x => $"Not in collection2: {x}");
            var diff2 = collection2.Except(collection1).Select(x => $"Not in collection1: {x}");

            return diff1.Concat(diff2);
        }

        public static string PrintCollection<T>(this IEnumerable<T> collection, string name)
        {
            return $"{name}: [{string.Join(", ", collection)}]";
        }
    }
}
