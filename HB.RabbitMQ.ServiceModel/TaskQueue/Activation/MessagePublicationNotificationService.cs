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
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext = false, IncludeExceptionDetailInFaults = true, MaxItemsInObjectGraph = int.MaxValue)]
    internal sealed partial class MessagePublicationNotificationService : IWasInteropService, IDisposable
    {
        private ConcurrentDictionary<int, Client> _appDomainHandlers = new ConcurrentDictionary<int, Client>();
        private readonly Timer _keepAliveTimer;
        private IRabbitMQQueueMonitor _queueMon;
        private readonly Func<IWasInteropServiceCallback> _getCallbackFunc;

        public MessagePublicationNotificationService(IRabbitMQQueueMonitor queueMonitor)
            : this(queueMonitor, () => OperationContext.Current.GetCallbackChannel<IWasInteropServiceCallback>())
        {
        }

        internal MessagePublicationNotificationService(IRabbitMQQueueMonitor queueMonitor, Func<IWasInteropServiceCallback> getCallbackFunc)
        {
            _getCallbackFunc = getCallbackFunc;
            _queueMon = queueMonitor;
            _queueMon.MessagePublished += QueueMonitor_MessagePublished;
            var keepAliveInterval = TimeSpan.FromMinutes(5);
            _keepAliveTimer = new Timer(state => FireKeepAlive(), null, keepAliveInterval, keepAliveInterval);
        }

        ~MessagePublicationNotificationService()
        {
            Dispose(false);
        }

        public void EnsureServiceAvailable(int listenerChannelId, string virtualPath)
        {
            var client = _appDomainHandlers.GetValueOrDefault(listenerChannelId);
            client?.Callback.EnsureServiceAvailable(virtualPath);
        }

        private void FireKeepAlive()
        {
            foreach (var client in _appDomainHandlers.Values)
            {
                client.Callback.KeepAlive();
            }
        }

        public void Register(int listenerChannelId, string applicationPath)
        {
            TraceInformation($"Register({nameof(listenerChannelId)}={listenerChannelId}, {nameof(applicationPath)}={applicationPath})", GetType());
            var callback = _getCallbackFunc();
            var client = new Client(callback, applicationPath);
            _appDomainHandlers.TryAdd(listenerChannelId, client);

            foreach (var queueName in _queueMon.GetQueuesWithPendingMessages(applicationPath))
            {
                if (!client.IsServiceActivated(queueName))
                {
                    callback.EnsureServiceAvailable(queueName);
                }
            }
        }

        public void ServiceActivated(int listenerChannelId, string virtualPath)
        {
            TraceInformation($"ServiceActivated({nameof(listenerChannelId)}={listenerChannelId}, {nameof(virtualPath)}={virtualPath})", GetType());
            var client = _appDomainHandlers.GetValueOrDefault(listenerChannelId);
            client?.AddActivatedService(virtualPath);
        }

        public void Unregister(int listenerChannelId)
        {
            TraceInformation($"Unregister({nameof(listenerChannelId)}={listenerChannelId})", GetType());
            _appDomainHandlers.Remove(listenerChannelId);
        }

        private void QueueMonitor_MessagePublished(object sender, MessagePublishedEventArgs e)
        {
            var clients = _appDomainHandlers.Values.Where(c => string.Equals(c.ApplicationPath, e.ApplicationPath, StringComparison.OrdinalIgnoreCase));
            foreach (var client in clients)
            {
                client.Callback.EnsureServiceAvailable(e.QueueName);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queueMon.MessagePublished -= QueueMonitor_MessagePublished;
                _keepAliveTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}