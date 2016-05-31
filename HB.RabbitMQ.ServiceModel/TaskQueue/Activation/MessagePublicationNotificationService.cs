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
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single, UseSynchronizationContext = false, IncludeExceptionDetailInFaults = true, MaxItemsInObjectGraph = int.MaxValue)]
    internal sealed partial class MessagePublicationNotificationService : IWasInteropService, IDisposable
    {
        private ConcurrentDictionary<Guid, Client> _appDomainHandlers = new ConcurrentDictionary<Guid, Client>();
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
            var keepAliveInterval = TimeSpan.FromMinutes(1);
            _keepAliveTimer = new Timer(state => FireKeepAlive(), null, TimeSpan.Zero, keepAliveInterval);
        }

        ~MessagePublicationNotificationService()
        {
            Dispose(false);
        }

        public void EnsureServiceAvailable(int listenerChannelId, string virtualPath)
        {
            var clients = _appDomainHandlers.Values.Where(c => c.ListenerChannelId == listenerChannelId);
            Parallel.ForEach(clients, client => client.EnsureServiceAvailable(virtualPath));
        }

        private void FireKeepAlive()
        {
            Parallel.ForEach(_appDomainHandlers.Values, client => client.KeepAlive());
        }

        public void Register(int listenerChannelId, Guid appDomainProcotolHandlerId, string applicationPath)
        {
            TraceInformation($"Register({nameof(listenerChannelId)}={listenerChannelId}, {nameof(appDomainProcotolHandlerId)}={appDomainProcotolHandlerId}, {nameof(applicationPath)}={applicationPath})", GetType());
            var callback = _getCallbackFunc();
            var client = new Client(this, callback, applicationPath, listenerChannelId, appDomainProcotolHandlerId);
            _appDomainHandlers.Add(appDomainProcotolHandlerId, client);
            foreach (var queueName in _queueMon.GetQueuesWithPendingMessages(applicationPath))
            {
                client.EnsureServiceAvailable(queueName);
            }
        }

        public void ServiceNotFound(Guid appDomainProcotolHandlerId, string virtualPath)
        {
            TraceInformation($"ServiceNotFound({nameof(appDomainProcotolHandlerId)}={appDomainProcotolHandlerId}, {nameof(virtualPath)}={virtualPath})", GetType());
            var client = _appDomainHandlers.GetValueOrDefault(appDomainProcotolHandlerId);
            client?.AddMissingService(virtualPath);
        }

        public void ServiceActivated(Guid appDomainProcotolHandlerId, string virtualPath)
        {
            TraceInformation($"ServiceActivated({nameof(appDomainProcotolHandlerId)}={appDomainProcotolHandlerId}, {nameof(virtualPath)}={virtualPath})", GetType());
            var client = _appDomainHandlers.GetValueOrDefault(appDomainProcotolHandlerId);
            client?.AddActivatedService(virtualPath);
        }

        public void Unregister(Guid appDomainProcotolHandlerId)
        {
            TraceInformation($"Unregister({nameof(appDomainProcotolHandlerId)}={appDomainProcotolHandlerId})", GetType());
            Client client;
            if (_appDomainHandlers.TryRemove(appDomainProcotolHandlerId, out client))
            {
                client.Dispose();
                TraceInformation($"Unregistered {nameof(appDomainProcotolHandlerId)} [{appDomainProcotolHandlerId}] for the application path [{client.ApplicationPath}] that was registered on [{client.CreationTime}].", GetType());
            }
        }

        private void QueueMonitor_MessagePublished(object sender, MessagePublishedEventArgs e)
        {
            var clients = _appDomainHandlers.Values.Where(c => string.Equals(c.ApplicationPath, e.ApplicationPath, StringComparison.OrdinalIgnoreCase));
            Parallel.ForEach(clients, client => client.EnsureServiceAvailable(e.QueueName));
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _queueMon.MessagePublished -= QueueMonitor_MessagePublished;
                _keepAliveTimer.Dispose();
                Parallel.ForEach(_appDomainHandlers.Values, client => client.Dispose());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}