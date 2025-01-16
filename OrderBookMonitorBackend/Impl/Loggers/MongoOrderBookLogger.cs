using Core.Shared.Domain.Models;
using MongoDB.Driver;
using OrderBookMonitorBackend.Interfaces;

namespace OrderBookMonitorBackend.Impl.Loggers
{
    public class MongoOrderBookLogger : IOrderBookLogger
    {
        private readonly IMongoCollection<OrderBookSnapshot> _snapshotCollection;

        public MongoOrderBookLogger(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("orderbook_db");
            _snapshotCollection = database.GetCollection<OrderBookSnapshot>("orderbook_snapshots");
        }

        public void LogSnapshot(OrderBookSnapshot snapshot)
        {
            _snapshotCollection.InsertOne(snapshot);
        }
    }
}
