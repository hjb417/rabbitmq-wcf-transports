using System;
using System.Diagnostics;

namespace HB.RabbitMQ.ServiceModel
{
    internal sealed class TimeoutTimer
    {
        private readonly Stopwatch _timer;
        private readonly TimeSpan _timeout;

        private TimeoutTimer(TimeSpan timeout)
        {
            _timeout = timeout;
            _timer = Stopwatch.StartNew();
        }

        public static TimeoutTimer StartNew(TimeSpan timeout)
        {
            return new TimeoutTimer(timeout);
        }

        public TimeSpan RemainingTime
        {
            get
            {
                if(_timeout == TimeSpan.MaxValue)
                {
                    return TimeSpan.MaxValue;
                }
                var remaining = _timeout - _timer.Elapsed;
                return (remaining.Ticks < 0) ? TimeSpan.Zero : remaining;
            }
        }

        public bool HasTimeRemaining { get { return RemainingTime.Ticks > 0; } }

        public void ThrowIfNoTimeRemaining()
        {
            if (!HasTimeRemaining)
            {
                throw new TimeoutException();
            }
        }
    }
}