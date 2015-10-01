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
using System.Threading;

namespace HB.RabbitMQ.ServiceModel.Throttling
{
    internal sealed class CpuAndMemoryDequeueThrottler : IDequeueThrottler
    {
        private readonly string _queueName;
        private readonly CpuAndMemoryHistoricalInfo _cpuAndMemInfo;
        private readonly Random _rand = new Random();

        public CpuAndMemoryDequeueThrottler(string queueName, CpuAndMemoryHistoricalInfo cpuAndMemInfo)
        {
            _queueName = queueName;
            _cpuAndMemInfo = cpuAndMemInfo;
        }

        public ThrottleResult Throttle(long messageCount, long counsumerCount, CancellationToken cancelToken)
        {
            TimeSpan minSleepTime = TimeSpan.FromSeconds(1);
            TimeSpan maxSleepTime = TimeSpan.FromMinutes(5);
            while (!cancelToken.IsCancellationRequested)
            {
                bool exit = true;
                if (GetHasHighCpuUsage())
                {
                    exit = false;
                    var sleepTime = TimeSpan.FromSeconds(_rand.Next((int)minSleepTime.TotalSeconds, (int)maxSleepTime.TotalSeconds));
                    Trace.TraceWarning("[{3}] Pausing dequeue of {0} for {2}s because CPU or memory usage is high on {1}.", _queueName, Environment.MachineName, sleepTime.TotalSeconds, GetType());
                    cancelToken.WaitHandle.WaitOne(sleepTime);
                }
                if (GetHasHighMemoryUsage())
                {
                    exit = false;
                    var sleepTime = TimeSpan.FromSeconds(_rand.Next((int)minSleepTime.TotalSeconds, (int)maxSleepTime.TotalSeconds));
                    Trace.TraceWarning("[{3}] Pausing dequeue of {0} for {2}s because CPU or memory usage is high on {1}.", _queueName, Environment.MachineName, sleepTime.TotalSeconds, GetType());
                    cancelToken.WaitHandle.WaitOne(sleepTime);
                }
                if (exit)
                {
                    return ThrottleResult.TakeMessage;
                }
            }
            return ThrottleResult.SkipMessage;
        }

        private bool GetHasHighCpuUsage()
        {
            return _cpuAndMemInfo.GetAverageCpuAndMemory().Cpu >= 97;
        }

        private bool GetHasHighMemoryUsage()
        {
            return _cpuAndMemInfo.GetAverageCpuAndMemory().Cpu >= 92;
        }

        public void Dispose()
        {
        }
    }
}