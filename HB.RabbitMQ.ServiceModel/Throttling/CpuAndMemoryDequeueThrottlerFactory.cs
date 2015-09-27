
namespace HB.RabbitMQ.ServiceModel.Throttling
{
    public class CpuAndMemoryDequeueThrottlerFactory : IDequeueThrottlerFactory
    {
        private static readonly CpuAndMemoryHistoricalInfo _cpuAndMemInfo = new CpuAndMemoryHistoricalInfo();

        public CpuAndMemoryDequeueThrottlerFactory()
        {
        }

        public IDequeueThrottler Create(string exchange, string queueName)
        {
            return new CpuAndMemoryDequeueThrottler(queueName, _cpuAndMemInfo);
        }
    }
}