using System;
using System.Diagnostics;

namespace HB.RabbitMQ.ServiceModel
{
    internal static class DisposeHelper
    {
        public static void DisposeIfNotNull(IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }

        public static void SilentDispose(IDisposable disposable)
        {
            if (disposable != null)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("[{0}] Failed to dispose object of type [{1}]. {2}", typeof(DisposeHelper), disposable.GetType(), e);
                }
            }
        }
    }
}