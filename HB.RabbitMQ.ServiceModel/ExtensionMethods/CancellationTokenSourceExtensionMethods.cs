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
using HB.RabbitMQ.ServiceModel;

namespace HB
{
    internal static class CancellationTokenSourceExtensionMethods
    {
        public static void CancelAfter(this CancellationTokenSource cancellationTokenSource, TimeSpan delay)
        {
            Timer timer = null;
            object disposeLock = new object();
            TimerCallback callback = state =>
            {
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch(Exception e)
                {
                    Trace.TraceWarning(string.Format("[{0}] Failed to cancel cancelation token source. {1}", typeof(CancellationTokenSourceExtensionMethods), e));
                }
                finally
                {
                    lock (disposeLock)
                    {
                        DisposeHelper.SilentDispose(timer);
                    }
                }
            };
            cancellationTokenSource.Token.Register(() =>
            {
                lock (disposeLock)
                {
                    DisposeHelper.DisposeIfNotNull(timer);
                }
            });
            timer = new Timer(callback, null, Timeout.Infinite, Timeout.Infinite);
            timer.Change(delay.ToMillisecondsTimeout(), Timeout.Infinite);
            if(cancellationTokenSource.Token.IsCancellationRequested)
            {
                lock (disposeLock)
                {
                    timer.Dispose();
                }
            }
        }
    }
}