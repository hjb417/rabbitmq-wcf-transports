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