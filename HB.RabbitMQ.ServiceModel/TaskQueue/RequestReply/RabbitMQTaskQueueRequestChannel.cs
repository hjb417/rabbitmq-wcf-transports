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
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueRequestChannel : RabbitMQTaskQueueChannelBase, IRequestChannel
    {
        private IRabbitMQReader _queueReader;
        private readonly Func<Message, TimeSpan, Message> _requestFunc;
        private readonly BufferManager _bufferMgr;

        public RabbitMQTaskQueueRequestChannel(
            BindingContext context,
            ChannelManagerBase channelManager,
            EndpointAddress remoteAddress,
            Uri via,
            BufferManager bufferManger,
            RabbitMQTaskQueueBinding binding
            )
            : base(context, channelManager, binding, new EndpointAddress(RabbitMQTaskQueueUri.Create(remoteAddress.Uri.Host, remoteAddress.Uri.Port, "r" + Guid.NewGuid().ToString("N"))))
        {
            MethodInvocationTrace.Write();
            RemoteAddress = remoteAddress;
            _bufferMgr = bufferManger;
            _requestFunc = Request;
            Via = via;
            RemoteUri = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
        }


        public RabbitMQTaskQueueUri RemoteUri { get; private set; }
        public EndpointAddress RemoteAddress { get; private set; }

        public Uri Via { get; private set; }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            DisposeHelper.DisposeIfNotNull(_queueReader);
            base.OnClose(timeout, closeReason);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timeoutTimer.RemainingTime);
            var connFactory = Binding.CreateConnectionFactory(RemoteAddress.Uri.Host, RemoteAddress.Uri.Port);
            if (Binding.AutoCreateServerQueue)
            {
                Binding.QueueReaderWriterFactory.CreateReader(connFactory, Binding.Exchange, RemoteUri.QueueName, Binding.IsDurable, false, Binding.TimeToLive, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions, Binding.MaxPriority).Dispose();
            }
            var localAddress = new RabbitMQTaskQueueUri(LocalAddress.Uri.ToString());
            _queueReader = Binding.QueueReaderWriterFactory.CreateReader(connFactory, Binding.Exchange, localAddress.QueueName, Binding.IsDurable, Binding.DeleteOnClose, Binding.TimeToLive, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions, null);
        }

        public IAsyncResult BeginRequest(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _requestFunc.BeginInvoke(message, timeout, callback, state);
        }

        public IAsyncResult BeginRequest(Message message, AsyncCallback callback, object state)
        {
            return BeginRequest(message, DefaultSendTimeout, callback, state);
        }

        public Message EndRequest(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _requestFunc.EndInvoke(result);
        }

        public Message Request(Message message, TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (ConcurrentOperationManager.TrackOperation())
            {
                if (message.Headers.To == null)
                {
                    message.Headers.To = RemoteAddress.Uri;
                }
                message.Headers.From = LocalAddress;
                bool isOneWayCall = true;
                if (message.Headers.ReplyTo != null)
                {
                    message.Headers.ReplyTo = LocalAddress;
                    isOneWayCall = false;
                }
                QueueWriter.Enqueue(Binding.Exchange, RemoteUri.QueueName, message, _bufferMgr, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
                return isOneWayCall
                    ? null
                    : _queueReader.Dequeue(Binding, MessageEncoderFactory, timeout, ConcurrentOperationManager.Token);
            }
        }

        public Message Request(Message message)
        {
            return Request(message, DefaultSendTimeout);
        }
    }
}
