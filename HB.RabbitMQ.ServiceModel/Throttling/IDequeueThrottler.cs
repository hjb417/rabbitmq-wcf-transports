using System;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel.Throttling
{
    public interface IDequeueThrottler : IDisposable
    {
        ThrottleResult Throttle(long messageCount, long counsumerCount, CancellationToken cancelToken);
    }
}