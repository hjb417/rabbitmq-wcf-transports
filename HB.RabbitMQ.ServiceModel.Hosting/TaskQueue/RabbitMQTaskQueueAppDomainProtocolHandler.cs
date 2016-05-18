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
using System.Diagnostics;
using System.ServiceModel;
using System.Web.Hosting;
using HB.RabbitMQ.ServiceModel.Hosting.ServiceModel;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    public sealed class RabbitMQTaskQueueAppDomainProtocolHandler : AppDomainProtocolHandler
    {
        private DuplexChannelFactory<IWasInteropService> _channelFactory;
        private IListenerChannelCallback _listenerChannelCallback;
        private IWasInteropService _wasInteropSvc;

        public RabbitMQTaskQueueAppDomainProtocolHandler()
        {
        }

        public override void StartListenerChannel(IListenerChannelCallback listenerChannelCallback)
        {
            var listenerChannelId = listenerChannelCallback.GetId();
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StartListenerChannel)}: Starting listener channel for channel id [{listenerChannelId}].");

            _listenerChannelCallback = listenerChannelCallback;
            var setup = listenerChannelCallback.GetBlobAsListenerChannelSetup();

            var callback = new WasInteropServiceCallback(listenerChannelId);
            _channelFactory = new DuplexChannelFactory<IWasInteropService>(new InstanceContext(callback), NetNamedPipeBindingFactory.Create(), new EndpointAddress(setup.MessagePublicationNotificationServiceUri));
            
            _wasInteropSvc = _channelFactory.CreateChannel();
            callback.Service = _wasInteropSvc;
            _wasInteropSvc.Register(listenerChannelId, setup.ApplicationPath);

            listenerChannelCallback.ReportStarted();
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StartListenerChannel)}: Started listener channel for channel id [{listenerChannelId}].");
        }

        public override void StopListenerChannel(int listenerChannelId, bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopListenerChannel)}: Stopping listener channel for channel id [{listenerChannelId}].");
            _wasInteropSvc.Unregister(listenerChannelId);
            _listenerChannelCallback.ReportStopped(0);
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopListenerChannel)}: Stopped listener channel for channel id [{listenerChannelId}].");
        }

        public override void StopProtocol(bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopProtocol)}: Stopping protocol.");
            _listenerChannelCallback.ReportStopped(0);
            HostingEnvironment.UnregisterObject(this);
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandler)}.{nameof(StopProtocol)}: Stopped protocol.");
        }
    }
}
