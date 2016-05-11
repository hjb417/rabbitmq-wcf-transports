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
using System.Text;
using System.Web.Hosting;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    public sealed class RabbitMQTaskQueueProcessProtocolHandler : ProcessProtocolHandler
    {
        private IAdphManager _adphManager;
        private const string _scheme = "hb.rmqtq";
        private readonly Dictionary<int, string> _appInstanceTable = new Dictionary<int, string>();

        public RabbitMQTaskQueueProcessProtocolHandler()
        {
        }

        public override void StartListenerChannel(IListenerChannelCallback listenerChannelCallback, IAdphManager adphManager)
        {
            _adphManager = adphManager;
            var channelId = listenerChannelCallback.GetId();
            var length = listenerChannelCallback.GetBlobLength();
            var blob = new byte[length];
            listenerChannelCallback.GetBlob(blob, ref length);
            var appId = Encoding.Unicode.GetString(blob);
            lock (_appInstanceTable)
            {
                _appInstanceTable[channelId] = appId;
            }
            adphManager.StartAppDomainProtocolListenerChannel(appId, _scheme, listenerChannelCallback);
        }

        public override void StopListenerChannel(int listenerChannelId, bool immediate)
        {
            string appId;
            lock(_appInstanceTable)
            {
                if (_appInstanceTable.TryGetValue(listenerChannelId, out appId))
                {
                    _adphManager.StopAppDomainProtocolListenerChannel(appId, _scheme, listenerChannelId, immediate);
                }
            }
        }

        public override void StopProtocol(bool immediate)
        { 
        }
    }
}