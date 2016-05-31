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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.Hosting;
using HB.RabbitMQ.ServiceModel.Hosting.ServiceModel;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.Configuration;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    public sealed class RabbitMQTaskQueueAppDomainProtocolHandler : AppDomainProtocolHandler
    {
        private DuplexChannelFactory<IWasInteropService> _channelFactory;
        private WasInteropServiceCallback _callback;
        private IListenerChannelCallback _listenerChannelCallback;
        private IWasInteropService _wasInteropSvc;
        private static readonly bool _autoStartServices;

        private static event EventHandler<EventArgs> MessageReceived;

        static RabbitMQTaskQueueAppDomainProtocolHandler()
        {
            var config = (RabbitMQTaskQueueWasConfigurationSection)ConfigurationManager.GetSection("rabbitMQTaskQueueWasConfiguration");
            if (config != null)
            {
                _autoStartServices = config.AutoStartServices;
            }
        }

        public RabbitMQTaskQueueAppDomainProtocolHandler()
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.ctor: Creating instance of AppDomain Protocol Handler.");
        }

        public static void ReportMessageReceived(Uri requestUri)
        {
            OnMessageReceived(EventArgs.Empty);
        }

        private static void OnMessageReceived(EventArgs e)
        {
            MessageReceived?.Invoke(null, e);
        }

        public override void StartListenerChannel(IListenerChannelCallback listenerChannelCallback)
        {
            var listenerChannelId = listenerChannelCallback.GetId();
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StartListenerChannel)}: Starting listener channel for channel id [{listenerChannelId}].");

            MessageReceived -= OnMessageReceived;
            MessageReceived += OnMessageReceived;
            _listenerChannelCallback = listenerChannelCallback;
            var setup = listenerChannelCallback.GetBlobAsListenerChannelSetup();

            _callback = new WasInteropServiceCallback();
            _channelFactory = new DuplexChannelFactory<IWasInteropService>(new InstanceContext(_callback), BindingFactory.Create(setup.MessagePublicationNotificationServiceUri), new EndpointAddress(setup.MessagePublicationNotificationServiceUri));

            _wasInteropSvc = _channelFactory.CreateChannel();
            _callback.Service = _wasInteropSvc;

            _wasInteropSvc.Register(listenerChannelId, _callback.Id, setup.ApplicationPath);

            if (_autoStartServices)
            {
                AutoStartServices(setup.ApplicationPath, _callback);
            }

            listenerChannelCallback.ReportStarted();
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StartListenerChannel)}: Started listener channel for channel id [{listenerChannelId}].");
        }

        private void AutoStartServices(string appName, IWasInteropServiceCallback callback)
        {
            if (!appName.EndsWith("/"))
            {
                appName += "/";
            }
            var svcPaths = Directory.GetFiles(HttpRuntime.AppDomainAppPath, "*.svc", SearchOption.AllDirectories)
                .Select(f => appName + f.Substring(HttpRuntime.AppDomainAppPath.Length));
            foreach (var svcPath in svcPaths)
            {
                callback.EnsureServiceAvailable(svcPath);
            }
        }

        private void OnMessageReceived(object sender, EventArgs e)
        {
            try
            {
                _listenerChannelCallback.ReportMessageReceived();
            }
            catch (Exception error)
            {
                Trace.TraceWarning($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(OnMessageReceived)}: {error}.");
            }
        }

        public override void StopListenerChannel(int listenerChannelId, bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopListenerChannel)}: Stopping listener channel for channel id [{listenerChannelId}].");
            MessageReceived -= OnMessageReceived;
            try
            {
                _wasInteropSvc.Unregister(_callback.Id);
            }
            catch (InvalidOperationException e) when (((ICommunicationObject)_wasInteropSvc).State != CommunicationState.Opened)
            {
                Trace.TraceWarning($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopListenerChannel)}: {e}.");
            }
            _channelFactory.Dispose();
            _listenerChannelCallback.ReportStopped(0);
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopListenerChannel)}: Stopped listener channel for channel id [{listenerChannelId}].");
        }

        public override void StopProtocol(bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopProtocol)}: Stopping protocol.");
            MessageReceived -= OnMessageReceived;
            _channelFactory.Dispose();
            _listenerChannelCallback.ReportStopped(0);
            HostingEnvironment.UnregisterObject(this);
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopProtocol)}: Stopped protocol.");
        }
    }
}