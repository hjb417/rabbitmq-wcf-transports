using System;
using System.Globalization;
using System.Reflection;
using System.Transactions;

namespace HB
{
    public static class AppDomainExtensionMethods
    {
        private const string TransactionDataName = "{3944FD09-2D47-4FF5-94FC-7D5AEC93FBA8}";

        public static AppDomain Clone(this AppDomain appDomain, string friendlyName)
        {
            return AppDomain.CreateDomain(friendlyName, null, AppDomain.CurrentDomain.SetupInformation);
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