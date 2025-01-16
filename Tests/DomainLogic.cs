using Core.Shared.Domain.Models;
using Core.Shared.Domain.Operations;
using System.Text;
using Tests.Models;
using Tests.Utils;

namespace Tests
{
    public class DomainLogic
    {
        private ChangeManager<OrderFromCryptoInc> _changeManager;
        private OrderBookManager _orderBookManager;
        private const int BookSize = 50;
        public DomainLogic()
        {
            _changeManager = new ChangeManager<OrderFromCryptoInc>(
                SelectPrice: o => o.SomePrice,
                SelectAmount: o => o.SomeAmount,
                ClassifyChange: OrderFromCryptoInc.ClassifyChange,
                bookSize: BookSize
            );

            _orderBookManager = new OrderBookManager(BookSize);
        }

        [Fact]
        public void TestUpdates()
        {
            var initialBids = CollectionUtils.GenerateNUnique(BookSize, OrderFromCryptoInc.CreateRandom, o => o.SomePrice);
            var initialAsks = CollectionUtils.GenerateNUnique(BookSize, OrderFromCryptoInc.CreateRandom, o => o.SomePrice);

            // Create some changes
            // 1
            var (bidToRemoveIndex, bidToRemove) = initialBids.PickRandom();
            bidToRemove.SomePrice = -1;

            // 2
            var bidToAdd = new OrderFromCryptoInc(
                somePrice: initialAsks.Select(a => a.SomePrice).GenerateUniqueDecimalNotInCollection(),
                someAmount: (decimal)0.91
            );

            // 3
            var (askToUpdateIndex, askToUpdate) = initialAsks.PickRandom();
            askToUpdate = new OrderFromCryptoInc(askToUpdate.SomePrice, askToUpdate.SomeAmount);

            var bidChanges = new List<OrderFromCryptoInc>() { bidToRemove, bidToAdd };
            var askChanges = new List<OrderFromCryptoInc>() { askToUpdate };
            //


            // Apply changes locally
            var bidsAfterUpdate = initialBids.Select(o => o.DeepCopy()).ToList();
            var asksAfterUpdate = initialAsks.Select(o => o.DeepCopy()).ToList();

            bidsAfterUpdate.RemoveAt(bidToRemoveIndex);
            bidsAfterUpdate.Add(bidToAdd);
            asksAfterUpdate[askToUpdateIndex] = askToUpdate;

            var bidsAfterUpdateDomain = bidsAfterUpdate.Select(o => o.ToDomain());
            var asksAfterUpdateDomain = asksAfterUpdate.Select(o => o.ToDomain());
            //

            // Apply using managers
            _orderBookManager.LoadInitial(
                initialBids.Select(o => o.ToDomain()),
                initialAsks.Select(o => o.ToDomain())
            );
            _changeManager.Prepare(initialBids, initialAsks);
            var diffObj = _changeManager.ProcessUpdate(bidChanges, askChanges);
            _orderBookManager.ApplyUpdate(diffObj);
            var orderBook = _orderBookManager.GetCurrentBook();
            var bidsAfterManagersUpdate = orderBook.Bids;
            var asksAfterManagersUpdate = orderBook.Asks;
            //

            string GenerateDescription()
            {
                var sb = new StringBuilder();

                sb.AppendLine(initialBids.PrintCollection(nameof(initialBids)));
                sb.AppendLine(initialAsks.PrintCollection(nameof(initialAsks)));

                sb.AppendLine($"Bid removed: {bidToRemove}");
                sb.AppendLine($"Bid added: {bidToAdd}");
                sb.AppendLine($"Ask updated: {askToUpdate}");

                sb.AppendLine(bidsAfterManagersUpdate.PrintCollection("Bids from manager"));
                sb.AppendLine(asksAfterManagersUpdate.PrintCollection("Asks from manager"));

                sb.AppendLine("But should be:");
                sb.AppendLine(bidsAfterUpdateDomain.PrintCollection("Bids"));
                sb.AppendLine(asksAfterUpdateDomain.PrintCollection("Asks"));

                return sb.ToString();
            }

            Assert.True(
                AreEqual(bidsAfterManagersUpdate, bidsAfterUpdateDomain),
                "BIDS LIST ARE NOT EQUAL!\n" +
                $"{GenerateDescription()}\n" +
                string.Join(", ", CollectionUtils.GetDifferences(bidsAfterManagersUpdate, bidsAfterUpdateDomain))
            );

            Assert.True(
                AreEqual(asksAfterManagersUpdate, asksAfterUpdateDomain),
                "ASKS LIST ARE NOT EQUAL!\n" +
                $"{GenerateDescription()}\n" +
                string.Join(", ", CollectionUtils.GetDifferences(asksAfterManagersUpdate, asksAfterUpdateDomain))
            );
        }

        [Fact]
        public void TestDiscardOrdersExceedingRangeLimits()
        {
            var initialBids = CollectionUtils.GenerateNUnique(BookSize + 10, OrderFromCryptoInc.CreateRandom, o => o.SomePrice);
            var initialAsks = CollectionUtils.GenerateNUnique(BookSize + 10, OrderFromCryptoInc.CreateRandom, o => o.SomePrice);

            var (bidsToTake, bidsToSkip) =
                (initialBids.OrderByDescending(o => o.SomePrice).Take(BookSize),
                initialBids.OrderByDescending(o => o.SomePrice).Skip(BookSize));

            var (asksToTake, asksToSkip) =
                (initialAsks.OrderBy(o => o.SomePrice).Take(BookSize),
                initialAsks.OrderBy(o => o.SomePrice).Skip(BookSize));

            // Apply changes locally
            var (bidsTrue, asksTrue) = (bidsToTake.Select(o => o.ToDomain()), asksToTake.Select(o => o.ToDomain()));

            // Apply using managers
            var (bids1, bids2) = (initialBids.Take(BookSize / 2), initialBids.Skip(BookSize / 2));
            var (asks1, asks2) = (initialAsks.Take(BookSize / 2), initialAsks.Skip(BookSize / 2));

            _orderBookManager.LoadInitial(
                bids1.Select(o => o.ToDomain()),
                asks1.Select(o => o.ToDomain())
            );
            _changeManager.Prepare(bids1, asks1);
            var diffObj = _changeManager.ProcessUpdate(bids2, asks2);
            _orderBookManager.ApplyUpdate(diffObj);
            var orderBook = _orderBookManager.GetCurrentBook();
            var bidsAfterManagersUpdate = orderBook.Bids;
            var asksAfterManagersUpdate = orderBook.Asks;
            //

            string GenerateDescription()
            {
                var sb = new StringBuilder();

                sb.AppendLine(initialBids.PrintCollection(nameof(initialBids)));
                sb.AppendLine(initialAsks.PrintCollection(nameof(initialAsks)));

                sb.AppendLine(bidsAfterManagersUpdate.PrintCollection("Bids from manager"));
                sb.AppendLine(asksAfterManagersUpdate.PrintCollection("Asks from manager"));

                sb.AppendLine("But should be:");
                sb.AppendLine(bidsTrue.PrintCollection("Bids"));
                sb.AppendLine(asksTrue.PrintCollection("Asks"));

                return sb.ToString();
            }

            Assert.True(
                AreEqual(bidsAfterManagersUpdate, bidsTrue),
                "BIDS LIST ARE NOT EQUAL!\n" +
                $"{GenerateDescription()}\n" +
                string.Join(", ", CollectionUtils.GetDifferences(bidsAfterManagersUpdate, bidsTrue))
            );

            Assert.True(
                AreEqual(asksAfterManagersUpdate, asksTrue),
                "ASKS LIST ARE NOT EQUAL!\n" +
                $"{GenerateDescription()}\n" +
                string.Join(", ", CollectionUtils.GetDifferences(asksAfterManagersUpdate, asksTrue))
            );
        }

        private static bool AreEqual(IEnumerable<Order> one, IEnumerable<Order> another)
        {
            if (one.Count() != another.Count()) return false;

            var first = one.OrderBy(o => o.Price).ThenBy(o => o.Amount);
            var second = another.OrderBy(o => o.Price).ThenBy(o => o.Amount);

            return first.Zip(second, (o1, o2) =>
            {
                return o1.Price == o2.Price && o1.Amount == o2.Amount;
            }).All(equal => equal);
        }
    }
}
