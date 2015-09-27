using HB.RabbitMQ.ServiceModel.Throttling;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal sealed class NoOpDequeueThrottlerFactory : IDequeueThrottlerFactory
    {
        public static readonly NoOpDequeueThrottlerFactory Instance = new NoOpDequeueThrottlerFactory();

        private NoOpDequeueThrottlerFactory()
        {
        }

        public IDequeueThrottler Create(string exchange, string queueName)
        {
            return NoOpDequeueThrottler.Instance;
        }
    }
}