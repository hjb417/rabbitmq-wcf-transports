using System;

namespace HB
{
    public static class ActionExtensionMethods
    {
        public static void TryInvoke<T>(this Action<T> action, T obj)
        {
            if(action != null)
            {
                action(obj);
            }
        }
    }
}