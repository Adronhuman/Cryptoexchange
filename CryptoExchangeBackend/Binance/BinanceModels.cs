using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace CryptoExchangeBackend.Binance
{
    public class OrderBook
    {
        public long LastUpdateId { get; set; }

        public List<Order> Bids { get; set; }

        public List<Order> Asks { get; set; }
    }

    public class Order
    {
        public string Price { get; set; }
        public string Quantity { get; set; }
    }

    public class DepthUpdateStreamEvent 
    {
        [JsonPropertyName("stream")]
        public string Stream { get; set; }

        [JsonPropertyName("data")]
        public DepthUpdate Data { get; set; }
    }

    public class DepthUpdate
    {
        [JsonPropertyName("e")]
        public string EventType { get; set; }

        [JsonPropertyName("E")]
        public long EventTime { get; set; }

        [JsonPropertyName("s")]
        public string Symbol { get; set; }

        [JsonPropertyName("U")]
        public long FirstUpdateId { get; set; }

        [JsonPropertyName("u")]
        public long LastUpdateId { get; set; }

        [JsonPropertyName("b")]
        public List<Order> Bids { get; set; }

        
        [JsonPropertyName("a")]
        public List<Order> Asks { get; set; }
    }

    public class OrderConverter : JsonConverter<Order>
    {
        public override Order Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                reader.Skip();
            }

            reader.Read();
            string price = reader.GetString();

            reader.Read();
            string quantity = reader.GetString();

            reader.Read();

            return new Order { Price = price, Quantity = quantity };
        }

        public override void Write(Utf8JsonWriter writer, Order value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.Price);
            writer.WriteStringValue(value.Quantity);
            writer.WriteEndArray();
        }
    }
}
