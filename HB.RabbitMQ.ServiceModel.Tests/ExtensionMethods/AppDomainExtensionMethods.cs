using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace HB
{
    public static class AppDomainExtensionMethods
    {
        private const string TransactionDataName = "{3944FD09-2D47-4FF5-94FC-7D5AEC93FBA8}";
        private const string DebugListenersName = "{01B3E5AB-FE51-4845-8383-90FFB6ED6ABB}";
        private const string TraceListenersName = "{62DAAD49-ED35-45E2-9890-5D722EB43B5A}";

        public static AppDomain Clone(this AppDomain appDomain, string friendlyName)
        {
            var clone = AppDomain.CreateDomain(friendlyName, null, AppDomain.CurrentDomain.SetupInformation);
            clone.SetData(DebugListenersName, Trace.Listeners.Cast<TraceListener>().ToArray());
            clone.SetData(TraceListenersName, Debug.Listeners.Cast<TraceListener>().ToArray());
            clone.DoCallBack(CopyListeners);
            clone.SetData(DebugListenersName, null);
            clone.SetData(TraceListenersName, null);
            return clone;
        }

        private static void CopyListeners()
        {
            var debugListeners = (TraceListener[])AppDomain.CurrentDomain.GetData(DebugListenersName);
            Debug.Listeners.AddRange(debugListeners);

            var traceListeners = (TraceListener[])AppDomain.CurrentDomain.GetData(TraceListenersName);
            Debug.Listeners.AddRange(traceListeners);
        }

        public static AppDomain Clone(this AppDomain appDomain)
        {
            return appDomain.Clone(string.Format("Clone Of [{0}]", AppDomain.CurrentDomain.FriendlyName));
        }

        public static void Unload(this AppDomain appDomain)
        {
            AppDomain.Unload(appDomain);
        }

        public static T CreateInstanceAndUnwrap<T>(this AppDomain appDomain)
        {
            var type = typeof(T);
            var instance = appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName);
            return (T)instance;
        }

        public static T CreateInstanceAndUnwrap<T>(this AppDomain appDomain, params object[] args)
        {
            var type = typeof(T);
            var instance = appDomain.CreateInstanceAndUnwrap(type.Assembly.FullName, type.FullName, false, BindingFlags.Public | BindingFlags.Instance, null, args, CultureInfo.CurrentCulture, null);
            return (T)instance;
        }

        public static void SetTransaction(this AppDomain appDomain, Transaction transaction)
        {
            appDomain.SetData(TransactionDataName, transaction);
            appDomain.DoCallBack(SetTransaction);
            appDomain.SetData(TransactionDataName, null);
        }

        private static void SetTransaction()
        {
            Transaction.Current = (Transaction)AppDomain.CurrentDomain.GetData(TransactionDataName);
        }
    }
}