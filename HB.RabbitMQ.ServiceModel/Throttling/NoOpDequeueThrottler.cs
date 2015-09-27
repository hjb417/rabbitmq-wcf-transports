using System.Threading;

namespace HB.RabbitMQ.ServiceModel.Throttling
{
    internal sealed class NoOpDequeueThrottler : IDequeueThrottler
    {
        public static readonly NoOpDequeueThrottler Instance = new NoOpDequeueThrottler();

        private NoOpDequeueThrottler()
        {
        }

        public ThrottleResult Throttle(long messageCount, long counsumerCount, CancellationToken cancelToken)
        {
            return ThrottleResult.TakeMessage;
        }

        public void Dispose()
        {
        }
    }
}
