using System;
using System.Diagnostics;
using System.Threading;

namespace HB
{
    public static class ManualResetEventSlimExtensionMethods
    {
        public static bool TrySet(this ManualResetEventSlim manualResetEventSlim)
        {
            try
            {
                manualResetEventSlim.Set();
                return true;
            }
            catch(Exception e)
            {
                Trace.TraceWarning("[{1}] Failed to set ManualResetEventSlim. {0}", e, typeof(ManualResetEventSlimExtensionMethods));
                return false;
            }
        }
    }
}