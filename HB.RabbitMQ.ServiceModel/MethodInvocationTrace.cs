using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel
{
    internal static class MethodInvocationTrace
    {
        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Write()
        {
            var method = new StackFrame(1).GetMethod();
            Debug.Assert(!method.IsGenericMethod);
            Debug.WriteLine("{3}-{4}: {0}.{1}({2})", method.DeclaringType.Name, method.Name, string.Join(", ", method.GetParameters().Select(p => p.Name)), DateTime.Now, Thread.CurrentThread.ManagedThreadId);
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Write<T>()
        {
            var method = new StackFrame(1).GetMethod();
            Debug.Assert(method.GetGenericArguments().Length == 1);
            Debug.WriteLine("{4}-{5}: {0}.{1}<{2}>({3})", method.DeclaringType.Name, method.Name, typeof(T).Name, string.Join(", ", method.GetParameters().Select(p => p.Name)), DateTime.Now, Thread.CurrentThread.ManagedThreadId);
        }
    }
}