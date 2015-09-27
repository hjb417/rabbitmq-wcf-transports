using System.IO;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    public sealed class DequeueResult
    {
        public DequeueResult(ulong deliveryTag, bool redelivered, string exchange, string routingKey, uint messageCount, IBasicProperties basicProperties, Stream body)
        {
            DeliveryTag = deliveryTag;
            Redelivered = redelivered;
            Exchange = exchange;
            RoutingKey = routingKey;
            MessageCount = messageCount;
            BasicProperties = basicProperties;
            Body = body;
        }

        public IBasicProperties BasicProperties { get; private set; }
        public Stream Body { get; private set; }
        public ulong DeliveryTag { get; private set; }
        public string Exchange { get; private set; }
        public uint MessageCount { get; private set; }
        public bool Redelivered { get; private set; }
        public string RoutingKey { get; private set; }
    }
}