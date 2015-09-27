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