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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.Activation.Microsoft.Win32.SafeHandles;

namespace HB.RabbitMQ.ServiceModel.Activation.Runtime.InteropServices
{
    internal static class ListenerAdapter
    {
        private static readonly SafeFreeLibrary _wasAPILib;
        private static readonly WebhostCloseAllListenerChannelInstances _webhostCloseAllListenerChannelInstances;
        private static readonly WebhostOpenListenerChannelInstance _webhostOpenListenerChannelInstance;
        private static readonly WebhostRegisterProtocol _webhostRegisterProtocol;
        private static readonly WebhostUnregisterProtocol _webhostUnregisterProtocol;

        static ListenerAdapter()
        {
            var libName = Path.Combine(Environment.SystemDirectory, @"inetsrv\wbhstipm.dll");
            _wasAPILib = NativeMethods.LoadLibraryEx(libName, IntPtr.Zero, LoadLibraryFlags.LOAD_WITH_ALTERED_SEARCH_PATH);
            _webhostRegisterProtocol = GetProcDelegate<WebhostRegisterProtocol>(_wasAPILib, "WebhostRegisterProtocol");
            _webhostUnregisterProtocol = GetProcDelegate<WebhostUnregisterProtocol>(_wasAPILib, "WebhostUnregisterProtocol");
            _webhostOpenListenerChannelInstance = GetProcDelegate<WebhostOpenListenerChannelInstance>(_wasAPILib, "WebhostOpenListenerChannelInstance");
            _webhostCloseAllListenerChannelInstances = GetProcDelegate<WebhostCloseAllListenerChannelInstances>(_wasAPILib, "WebhostCloseAllListenerChannelInstances");
        }

        private static TDelegate GetProcDelegate<TDelegate>(SafeFreeLibrary lib, string procName)
        {
            IntPtr funcPtr = NativeMethods.GetProcAddress(lib, procName);
            if (funcPtr == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return (TDelegate)(object)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(TDelegate));
        }

        public static WebHostProtocolSafeHandle RegisterProtocol(string protocolId, ref WebhostListenerCallbacks listenerCallbacks)
        {
            int protocolHandle;
            var hresult = _webhostRegisterProtocol(protocolId, ref listenerCallbacks, IntPtr.Zero, out protocolHandle);
            if (hresult != HRESULT.S_OK)
            {
                throw new Win32Exception(hresult);
            }
            return new WebHostProtocolSafeHandle(protocolHandle);
        }

        public static void UnregisterProtocol(int protocolHandle)
        {
            int hresult = _webhostUnregisterProtocol(protocolHandle);
            if (hresult != HRESULT.S_OK)
            {
                throw new Win32Exception(hresult);
            }
        }

        public static void OpenListenerChannelInstance(WebHostProtocolSafeHandle protocolHandle, string appPoolId, int listenerChannelId, byte[] queueBlob)
        {
            int queueBlobLength = (queueBlob == null) ? 0 :queueBlob.Length;
            bool addedRef = false;
            try
            {
                protocolHandle.DangerousAddRef(ref addedRef);
                if(!addedRef)
                {
                    throw new ArgumentException("The protocol handle has been disposed.", nameof(protocolHandle));
                }
                var hresult = _webhostOpenListenerChannelInstance(protocolHandle.DangerousGetHandle().ToInt32(), appPoolId, listenerChannelId, queueBlob, queueBlobLength);
                if(hresult != HRESULT.S_OK)
                {
                    throw new Win32Exception(hresult);
                }

            }
            finally
            {
                if (addedRef)
                {
                    protocolHandle.DangerousRelease();
                }
            }
        }

        public static int CloseAllListenerChannelInstances(int protocolHandle, string appPoolId, int listenerChannelId)
        {
            if (string.IsNullOrEmpty(appPoolId))
            {
                throw new ArgumentNullException(nameof(appPoolId));
            }
            
            return _webhostCloseAllListenerChannelInstances(protocolHandle, appPoolId, listenerChannelId);
        }
    }
}
