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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Activation.Runtime.InteropServices;

namespace HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter
{
    public class ListenerAdapter : IDisposable
    {
        private WebhostListenerCallbacks _callbacks;
        private WebHostProtocolSafeHandle _protocolHandle;
        private readonly ManualResetEventSlim _initCompleteEvent = new ManualResetEventSlim();
        private readonly string _scheme;
        private volatile bool _disposed;

        public event EventHandler<WindowsProcessActivationServiceDisconnectedEventArgs> WindowsProcessActivationServiceDisconnected;
        public event EventHandler WindowsProcessActivationServiceConnected;
        public event EventHandler<ApplicationRequestBlockedStateChangedEventArgs> ApplicationRequestBlockedStateChanged;
        public event EventHandler<ApplicationPoolStateChangedEventArgs> ApplicationPoolStateChanged;
        public event EventHandler<ApplicationPoolIdentityChangedEventArgs> ApplicationPoolIdentityChanged;
        public event EventHandler<ApplicationPoolCreatedEventArgs> ApplicationPoolCreated;
        public event EventHandler<ApplicationPoolDeletedEventArgs> ApplicationPoolDeleted;
        public event EventHandler<ApplicationPoolListenerChannelInstanceEventArgs> ApplicationPoolListenerChannelInstancesStopped;
        public event EventHandler<ApplicationPoolListenerChannelInstanceEventArgs> ApplicationPoolCanOpenNewListenerChannelInstance;
        public event EventHandler<ApplicationDeletedEventArgs> ApplicationDeleted;
        public event EventHandler<ApplicationCreatedEventArgs> ApplicationCreated;
        public event EventHandler<ApplicationBindingsChangedEventArgs> ApplicationBindingsChanged;
        public event EventHandler<ApplicationAppPoolChangedEventArgs> ApplicationAppPoolChanged;

        public ListenerAdapter(string scheme)
        {
            _scheme = scheme;
            _callbacks = new WebhostListenerCallbacks();
            _callbacks.BytesInCallbackStructure = Marshal.SizeOf(_callbacks);
            _callbacks.ApplicationAppPoolChanged = OnApplicationAppPoolChanged;
            _callbacks.ApplicationBindingsChanged = OnApplicationBindingsChanged;
            _callbacks.ApplicationCreated = OnApplicationCreated;
            _callbacks.ApplicationDeleted = OnApplicationDeleted;
            _callbacks.ApplicationPoolAllListenerChannelInstancesStopped = OnApplicationPoolAllListenerChannelInstancesStopped;
            _callbacks.ApplicationPoolCanOpenNewListenerChannelInstance = OnApplicationPoolCanOpenNewListenerChannelInstance;
            _callbacks.ApplicationPoolCreated = OnApplicationPoolCreated;
            _callbacks.ApplicationPoolDeleted = OnApplicationPoolDeleted;
            _callbacks.ApplicationPoolIdentityChanged = OnApplicationPoolIdentityChanged;
            _callbacks.ApplicationPoolStateChanged = OnApplicationPoolStateChanged;
            _callbacks.ApplicationRequestsBlockedChanged = OnApplicationRequestsBlockedChanged;
            _callbacks.ConfigManagerConnected = OnConfigManagerConnected;
            _callbacks.ConfigManagerDisconnected = OnConfigManagerDisconnected;
            _callbacks.ConfigManagerInitializationCompleted = OnConfigManagerInitializationCompleted;
        }

        ~ListenerAdapter()
        {
            Dispose(false);
        }

        public void Initialize()
        {
            if (_protocolHandle != null)
            {
                throw new InvalidOperationException($"The {nameof(Initialize)} method has already been invoked.");
            }
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            Trace.TraceInformation($"Registering the protocol [{_scheme}].");
            _protocolHandle = Runtime.InteropServices.ListenerAdapter.RegisterProtocol(_scheme, ref _callbacks);
            Trace.TraceInformation($"Waiting for the registration of the protocol [{_scheme}] to finish.");
            _initCompleteEvent.Wait();
            Trace.TraceInformation($"Registered the protocol [{_scheme}].");
        }

        public void OpenListenerChannelInstance(string applicationPoolId, int listenerChannelId, byte[] queueBlob)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
            Runtime.InteropServices.ListenerAdapter.OpenListenerChannelInstance(_protocolHandle, applicationPoolId, listenerChannelId, queueBlob);
        }

        private void OnConfigManagerInitializationCompleted(IntPtr context)
        {
            Trace.TraceInformation(nameof(OnConfigManagerInitializationCompleted));
            _initCompleteEvent.Set();
        }

        private void OnConfigManagerDisconnected(IntPtr context, int hresult)
        {
            Trace.TraceInformation($"{nameof(OnConfigManagerDisconnected)} [{nameof(hresult)}={hresult}]");
            var error = (hresult == 0) ? null : new Win32Exception(hresult);
            WindowsProcessActivationServiceDisconnected?.Invoke(this, new WindowsProcessActivationServiceDisconnectedEventArgs(error));
        }

        private void OnConfigManagerConnected(IntPtr context)
        {
            Trace.TraceInformation(nameof(OnConfigManagerConnected));
            WindowsProcessActivationServiceConnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnApplicationRequestsBlockedChanged(IntPtr context, string appKey, bool requestsBlocked)
        {
            Trace.TraceInformation($"{nameof(OnApplicationRequestsBlockedChanged)} [{nameof(appKey)}={appKey}, {nameof(requestsBlocked)}={requestsBlocked}]");
            var state = requestsBlocked ? ApplicationRequestsBlockedStates.Blocked : ApplicationRequestsBlockedStates.Processsed;
            ApplicationRequestBlockedStateChanged?.Invoke(this, new ApplicationRequestBlockedStateChangedEventArgs(appKey, state));
        }

        private void OnApplicationPoolStateChanged(IntPtr context, string appPoolId, bool isEnabled)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolStateChanged)} [{nameof(appPoolId)}={appPoolId}, {nameof(isEnabled)}={isEnabled}]");
            var state = isEnabled ? ApplicationPoolStates.Enabled : ApplicationPoolStates.Disabled;
            ApplicationPoolStateChanged?.Invoke(this, new ApplicationPoolStateChangedEventArgs(appPoolId, state));
        }

        private void OnApplicationPoolIdentityChanged(IntPtr context, string appPoolId, IntPtr sid)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolIdentityChanged)} [{nameof(appPoolId)}={appPoolId}, {nameof(sid)}={sid}]");
            ApplicationPoolIdentityChanged?.Invoke(this, new ApplicationPoolIdentityChangedEventArgs(appPoolId, new SecurityIdentifier(sid)));
        }

        private void OnApplicationPoolDeleted(IntPtr context, string appPoolId)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolDeleted)} [{nameof(appPoolId)}={appPoolId}]");
            ApplicationPoolDeleted?.Invoke(this, new ApplicationPoolDeletedEventArgs(appPoolId));
        }

        private void OnApplicationPoolCreated(IntPtr context, string appPoolId, IntPtr sid)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolCreated)} [{nameof(appPoolId)}={appPoolId}, {nameof(sid)}={sid}]");
            ApplicationPoolCreated?.Invoke(this, new ApplicationPoolCreatedEventArgs(appPoolId, new SecurityIdentifier(sid)));
        }

        private void OnApplicationPoolCanOpenNewListenerChannelInstance(IntPtr context, string appPoolId, int listenerChannelId)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolCanOpenNewListenerChannelInstance)} [{nameof(appPoolId)}={appPoolId}, {nameof(listenerChannelId)}={listenerChannelId}]");
            ApplicationPoolCanOpenNewListenerChannelInstance?.Invoke(this, new ApplicationPoolListenerChannelInstanceEventArgs(appPoolId, listenerChannelId));
        }

        private void OnApplicationPoolAllListenerChannelInstancesStopped(IntPtr context, string appPoolId, int listenerChannelId)
        {
            Trace.TraceInformation($"{nameof(OnApplicationPoolAllListenerChannelInstancesStopped)} [{nameof(appPoolId)}={appPoolId}, {nameof(listenerChannelId)}={listenerChannelId}]");
            ApplicationPoolListenerChannelInstancesStopped?.Invoke(this, new ApplicationPoolListenerChannelInstanceEventArgs(appPoolId, listenerChannelId));
        }

        private void OnApplicationDeleted(IntPtr context, string appKey)
        {
            Trace.TraceInformation($"{nameof(OnApplicationDeleted)} [{nameof(context)}={context}, {nameof(appKey)}={appKey}]");
            ApplicationDeleted?.Invoke(this, new ApplicationDeletedEventArgs(appKey));
        }

        private void OnApplicationCreated(IntPtr context, string appKey, string path, int siteId, string appPoolId, IntPtr bindingsMultiSz, int numberOfBindings, bool requestsBlocked)
        {
            var bindings = ParseBindings(bindingsMultiSz, numberOfBindings);
            var blockedState = requestsBlocked ? ApplicationRequestsBlockedStates.Blocked : ApplicationRequestsBlockedStates.Processsed;
            Trace.TraceInformation($"{nameof(OnApplicationCreated)} [{nameof(context)}={context}, {nameof(appKey)}={appKey}, {nameof(path)}={path}, {nameof(siteId)}={siteId}, {nameof(appPoolId)}={appPoolId}, {nameof(bindings)}={string.Join(",", bindings)}, {nameof(blockedState)}={blockedState}]");
            ApplicationCreated?.Invoke(this, new ApplicationCreatedEventArgs(appKey, path, siteId, appPoolId, bindings, blockedState));
        }

        private void OnApplicationBindingsChanged(IntPtr context, string appKey, IntPtr bindingsMultiSz, int numberOfBindings)
        {
            Trace.TraceInformation($"{nameof(OnApplicationBindingsChanged)} [{nameof(context)}={context}, {nameof(appKey)}={appKey}, {nameof(bindingsMultiSz)}={bindingsMultiSz}, {nameof(numberOfBindings)}={numberOfBindings}]");
            var bindings = ParseBindings(bindingsMultiSz, numberOfBindings);
            ApplicationBindingsChanged?.Invoke(this, new ApplicationBindingsChangedEventArgs(appKey, bindings));
        }

        private void OnApplicationAppPoolChanged(IntPtr context, string appKey, string appPoolId)
        {
            Trace.TraceInformation($"{nameof(OnApplicationAppPoolChanged)} [{nameof(context)}={context}, {nameof(appKey)}={appKey}, {nameof(appPoolId)}={appPoolId}]");
            ApplicationAppPoolChanged?.Invoke(this, new ApplicationAppPoolChangedEventArgs(appKey, appPoolId));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_protocolHandle != null)
                {
                    _protocolHandle.Dispose();
                }
                _initCompleteEvent.Dispose();

                WindowsProcessActivationServiceDisconnected = null;
                WindowsProcessActivationServiceConnected = null;
                ApplicationRequestBlockedStateChanged = null;
                ApplicationPoolStateChanged = null;
                ApplicationPoolIdentityChanged = null;
                ApplicationPoolCreated = null;
                ApplicationPoolDeleted = null;
                ApplicationPoolListenerChannelInstancesStopped = null;
                ApplicationPoolCanOpenNewListenerChannelInstance = null;
                ApplicationDeleted = null;
                ApplicationCreated = null;
                ApplicationBindingsChanged = null;
                ApplicationAppPoolChanged = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        private string[] ParseBindings(IntPtr bindingsMultiSz, int numberOfBindings)
        {
            if (bindingsMultiSz == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(bindingsMultiSz));
            }
            if (numberOfBindings < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfBindings), numberOfBindings, "The number of bindings must be equal to or greater than 0.");
            }
            var array = new string[numberOfBindings];
            var ptr = bindingsMultiSz;
            for (int i = 0; i < numberOfBindings; i++)
            {
                string text = Marshal.PtrToStringUni(ptr);
                if (string.IsNullOrEmpty(text))
                {
                    throw new ArgumentException("The bindings cannot contain null or empty strings.", nameof(bindingsMultiSz));
                }
                array[i] = text;
                ptr += text.Length + 1;
            }
            return array;
        }
    }
}