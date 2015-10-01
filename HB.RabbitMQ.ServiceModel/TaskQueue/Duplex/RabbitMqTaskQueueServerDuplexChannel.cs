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
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueServerDuplexChannel : RabbitMQTaskQueueDuplexChannelBase
    {
        private readonly EndpointAddress _localAddress;
        private readonly EndpointAddress _remoteAddress;
        private readonly IRabbitMQReader _queueReader;
        private readonly RabbitMQTaskQueueUri _remoteUri;
        private readonly BufferManager _bufferMgr;

        public RabbitMQTaskQueueServerDuplexChannel(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, EndpointAddress remoteAddress, BufferManager bufferManager, IRabbitMQReader queueReader)
            : base(context, channelManager, binding, remoteAddress)
        {
            MethodInvocationTrace.Write();
            _bufferMgr = bufferManager;
            _remoteAddress = remoteAddress;
            _remoteUri = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
            _localAddress = localAddress;
            _queueReader = queueReader;
        }

        public override EndpointAddress LocalAddress { get { return _localAddress; } }
        protected override IRabbitMQReader QueueReader { get { return _queueReader; } }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (var msg = Message.CreateMessage(MessageVersion.Default, Actions.InputSessionClosingRequest, new InputSessionClosingRequest()))
            {
                try
                {
                    var sendTimeout = timeoutTimer.RemainingTime;
                    if (closeReason == CloseReasons.Abort)
                    {
                        sendTimeout = TimeSpanHelper.Max(sendTimeout, TimeSpan.FromSeconds(30));
                    }
                    Send(msg, sendTimeout);
                }
                catch (Exception e)
                {
                    bool rethrow = true;
                    if (closeReason == CloseReasons.Abort)
                    {
                        rethrow = false;
                    }
                    if(e is RemoteQueueDoesNotExistException)
                    {
                        rethrow = false;
                    }
                    Trace.TraceWarning("[{2}] Failed to notify the endpoint [{0}] that the queue is closing. {1}", _remoteUri, e, GetType());
                    if(rethrow)
                    {
                        throw;
                    }
                }
            }
            base.OnClose(timeoutTimer.RemainingTime, closeReason);
            DisposeHelper.DisposeIfNotNull(_queueReader);
        }

        public override void Send(Message message, TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            using (ConcurrentOperationManager.TrackOperation())
            {
                QueueWriter.Enqueue(_remoteUri.Exchange, _remoteUri.QueueName, message, _bufferMgr, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeout, ConcurrentOperationManager.Token);
            }
        }
    }
}