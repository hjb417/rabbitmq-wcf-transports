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
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;
using RabbitMQ.Client.Events;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueServerDuplexChannel : RabbitMQTaskQueueDuplexChannelBase
    {
        private readonly EndpointAddress _remoteAddress;
        private readonly IRabbitMQReader _queueReader;
        private readonly RabbitMQTaskQueueUri _remoteUri;
        private readonly BufferManager _bufferMgr;
        private readonly string _abortTopic;
        private readonly EventingBasicConsumer _clientClosedEvent;

        public RabbitMQTaskQueueServerDuplexChannel(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, EndpointAddress remoteAddress, BufferManager bufferManager, IRabbitMQReader queueReader, string abortTopic, EventingBasicConsumer clientClosedEvent)
            : base(context, channelManager, binding, remoteAddress, localAddress)
        {
            MethodInvocationTrace.Write();
            _abortTopic = abortTopic;
            _clientClosedEvent = clientClosedEvent;
            _bufferMgr = bufferManager;
            _remoteAddress = remoteAddress;
            _remoteUri = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
            _clientClosedEvent.Received += ClientMessageReceived;

            _queueReader = queueReader;
        }

        private void ClientMessageReceived(object sender, BasicDeliverEventArgs e)
        {
            if (e.RoutingKey == _abortTopic)
            {
                _clientClosedEvent.Received -= ClientMessageReceived;
                CloseSessionRequestReceived = true;
                Abort();
            }
        }

        protected override IRabbitMQReader QueueReader { get { return _queueReader; } }

        public override Message Receive(TimeSpan timeout)
        {
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            var msg = base.Receive(timeoutTimer.RemainingTime);
            if (CloseSessionRequestReceived)
            {
                Close(timeoutTimer.RemainingTime);
            }
            return msg;
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            bool isAborting = closeReason == CloseReasons.Abort;
            using (ConcurrentOperationManager.TrackOperation())
            {
                try
                {
                    _clientClosedEvent.Received -= ClientMessageReceived;
                    CloseOutputSession(isAborting ? TimeSpan.FromSeconds(30) : timeoutTimer.RemainingTime, !isAborting);
                }
                catch (Exception e)
                {
                    bool rethrow = true;
                    if (closeReason == CloseReasons.Abort)
                    {
                        rethrow = false;
                    }
                    if (e is RemoteQueueDoesNotExistException)
                    {
                        rethrow = false;
                    }
                    Trace.TraceWarning("[{2}] Failed to notify the endpoint [{0}] that the queue is closing. {1}", _remoteUri, e, GetType());
                    if (rethrow)
                    {
                        throw;
                    }
                }
            }
            try
            {
                base.OnClose(timeoutTimer.RemainingTime, closeReason);
            }
            finally
            {
                DisposeHelper.DisposeIfNotNull(_queueReader);
            }
        }

        public override void Send(Message message, TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            using (ConcurrentOperationManager.TrackOperation())
            {
                QueueWriter.Enqueue(Binding.Exchange, _remoteUri.QueueName, message, _bufferMgr, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeout, ConcurrentOperationManager.Token);
            }
        }
    }
}
