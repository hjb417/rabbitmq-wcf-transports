using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HB
{
    internal static class BlockingCollectionExtensionMethods
    {
        public static bool TryTake<T>(this BlockingCollection<T> collection, out T item, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var timeoutInMs = Math.Min(timeout.TotalMilliseconds, int.MaxValue);
            return collection.TryTake(out item, (int) timeoutInMs, cancellationToken);
        }
    }
}