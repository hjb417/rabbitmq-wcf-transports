using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel.Throttling
{
    internal sealed class CpuAndMemoryHistoricalInfo : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private readonly PerformanceCounter _memCounter = new PerformanceCounter("Memory", "Available MBytes");
        private readonly Timer _refreshTimer;
        private readonly LinkedList<float> _cpuHistory = new LinkedList<float>();
        private readonly LinkedList<float> _memHistory = new LinkedList<float>();
        private readonly object _pollLock = new object();
        private volatile bool _isDisposed;
        private readonly long _maxHistory;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();
        private static readonly ulong _memAmt = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;

        public CpuAndMemoryHistoricalInfo()
        {
            var interval = TimeSpan.FromSeconds(1);
            var sampleLength = TimeSpan.FromMinutes(1);
            _maxHistory = sampleLength.Ticks / interval.Ticks;
            _refreshTimer = new Timer(state => PollPerformance(), null, interval, interval);
            PollPerformance();
        }

        public CpuAndMemoryLoad GetAverageCpuAndMemory()
        {
            _rwLock.EnterReadLock();
            try
            {
                var avgCpu =_cpuHistory.Average();
                var avgMem = _memHistory.Average() / _memAmt;
                return new CpuAndMemoryLoad(avgCpu, avgMem);
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }

        private void PollPerformance()
        {
            bool gotLock = false;
            try
            {
                Monitor.TryEnter(_pollLock, ref gotLock);
                if(!gotLock)
                {
                    return;
                }
                var cpu = _cpuCounter.NextValue();
                var memory = _memCounter.NextValue();
                try
                {
                    _rwLock.EnterWriteLock();
                    try
                    {
                        _cpuHistory.AddLast(_cpuCounter.NextValue());
                        _memHistory.AddLast(_memCounter.NextValue());
                        if (_cpuHistory.Count > _maxHistory)
                        {
                            _cpuHistory.RemoveFirst();
                        }
                        if (_memHistory.Count > _maxHistory)
                        {
                            _memHistory.RemoveFirst();
                        }
                    }
                    finally
                    {
                        _rwLock.ExitWriteLock();
                    }
                }
                catch
                {
                    if (!_isDisposed)
                    {
                        throw;
                    }
                }
            }
            finally
            {
                if(gotLock)
                {
                    Monitor.Exit(_pollLock);
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
            _refreshTimer.Dispose();
            _cpuCounter.Dispose();
            _memCounter.Dispose();
        }
    }
}