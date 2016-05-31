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
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Hosting;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    public sealed class RabbitMQTaskQueueProcessProtocolHandler : ProcessProtocolHandler
    {
        private IAdphManager _adphManager;
        private readonly Dictionary<int, ListenerChannelSetup> _appInstanceTable = new Dictionary<int, ListenerChannelSetup>();

        public RabbitMQTaskQueueProcessProtocolHandler()
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.ctor: Creating instance of Process Protocol Handler.");
        }

        public override void StartListenerChannel(IListenerChannelCallback listenerChannelCallback, IAdphManager adphManager)
        {
            _adphManager = adphManager;
            var channelId = listenerChannelCallback.GetId();

            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.{nameof(StartListenerChannel)}: Starting listener channel for channel id [{channelId}].");

            var setup = listenerChannelCallback.GetBlobAsListenerChannelSetup();
            lock (_appInstanceTable)
            {
                if (!_appInstanceTable.ContainsKey(channelId))
                {
                    _appInstanceTable.Add(channelId, setup);
                }
            }
            adphManager.StartAppDomainProtocolListenerChannel(setup.ApplicationId, Constants.Scheme, listenerChannelCallback);
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.{nameof(StartListenerChannel)}: Started listener channel for channel id [{channelId}].");
        }

        public override void StopListenerChannel(int listenerChannelId, bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.{nameof(StopListenerChannel)}: Stopping listener channel for channel id [{listenerChannelId}].");
            ListenerChannelSetup channelSetup;
            lock(_appInstanceTable)
            {
                if (_appInstanceTable.TryGetValue(listenerChannelId, out channelSetup))
                {
                    _adphManager.StopAppDomainProtocolListenerChannel(channelSetup.ApplicationId, Constants.Scheme, listenerChannelId, immediate);
                }
            }
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.{nameof(StopListenerChannel)}: Stopped listener channel for channel id [{listenerChannelId}].");
        }

        public override void StopProtocol(bool immediate)
        {
            Trace.TraceInformation($"{nameof(RabbitMQTaskQueueProcessProtocolHandler)}.{nameof(StopProtocol)}."); 
        }
    }
}