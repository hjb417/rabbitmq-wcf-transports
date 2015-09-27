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