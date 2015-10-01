/*
Copyright (c) 2015 HJB417

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
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