namespace HB.RabbitMQ.ServiceModel.Throttling
{
    public interface IDequeueThrottlerFactory
    {
        IDequeueThrottler Create(string exchange, string queueName);
    }
}