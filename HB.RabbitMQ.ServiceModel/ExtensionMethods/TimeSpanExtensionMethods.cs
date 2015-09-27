using System;
using System.Threading;

namespace HB
{
    public static class TimeSpanExtensionMethods
    {
        public static int ToMillisecondsTimeout(this TimeSpan timeSpan)
        {
            if(timeSpan == TimeSpan.MaxValue)
            {
                return Timeout.Infinite;
            }
            return (int)Math.Min(int.MaxValue, timeSpan.TotalMilliseconds);
        }
    }
}