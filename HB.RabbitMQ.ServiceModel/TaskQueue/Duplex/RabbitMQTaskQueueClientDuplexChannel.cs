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
using System.Xml;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueClientDuplexChannel<TChannel> : RabbitMQTaskQueueDuplexChannelBase
        where TChannel : IChannel
    {
        private readonly RabbitMQTaskQueueUri _remoteAddress;
        private IRabbitMQReader _queueReader;
        private readonly BufferManager _bufferMgr;
        private RabbitMQTaskQueueUri _remoteSessionUri;

        public RabbitMQTaskQueueClientDuplexChannel(BindingContext context, RabbitMQTaskQueueDuplexChannelFactory<TChannel> channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, EndpointAddress remoteAddress, BufferManager bufferManager)
            : base(context, channelManager, binding, remoteAddress, localAddress)
        {
            MethodInvocationTrace.Write();
            _bufferMgr = bufferManager;
            _remoteAddress = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
        }

        private new RabbitMQTaskQueueDuplexChannelFactory<TChannel> Manager { get { return (RabbitMQTaskQueueDuplexChannelFactory<TChannel>)base.Manager; } }
        protected override IRabbitMQReader QueueReader { get { return _queueReader; } }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timer.RemainingTime);

            var localUri = new RabbitMQTaskQueueUri(LocalAddress.Uri.ToString());
            using (ConcurrentOperationManager.TrackOperation())
            using (var createSessionReqMsg = Message.CreateMessage(MessageVersion.Default, Actions.CreateSessionRequest, new CreateSessionRequest()))
            {
                createSessionReqMsg.Headers.ReplyTo = LocalAddress;
                createSessionReqMsg.Headers.From = LocalAddress;
                createSessionReqMsg.Headers.To = RemoteAddress.Uri;
                createSessionReqMsg.Headers.MessageId = new UniqueId();

                var connFactory = Binding.CreateConnectionFactory(RemoteAddress.Uri.Host, RemoteAddress.Uri.Port);

                try
                {
                    _queueReader = Binding.QueueReaderWriterFactory.CreateReader(connFactory, Binding.Exchange, localUri.QueueName, Binding.IsDurable, Binding.DeleteOnClose, Binding.TimeToLive, timer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions, null);
                    QueueWriter.Enqueue(Binding.Exchange, _remoteAddress.QueueName, createSessionReqMsg, _bufferMgr, Binding, MessageEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
                    using (var msg = _queueReader.Dequeue(Binding, MessageEncoderFactory, timer.RemainingTime, ConcurrentOperationManager.Token))
                    {
                        var response = msg.GetBody<CreateSessionResponse>();
                        _remoteSessionUri = new RabbitMQTaskQueueUri(msg.Headers.ReplyTo.Uri.ToString());
                    }
                }
                catch
                {
                    DisposeHelper.DisposeIfNotNull(_queueReader);
                    throw;
                }
            }
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            if (!CloseSessionRequestReceived)
            {
                try
                {
                    //when aborting and timeout=zero, set timeout to 30s to try to notify server that client is aborting.
                    var closeSessionTimeout = (closeReason == CloseReasons.Abort)
                        ? TimeSpanHelper.Max(timeoutTimer.RemainingTime, TimeSpan.FromSeconds(30))
                        : timeoutTimer.RemainingTime;

                    CloseOutputSession(closeSessionTimeout);
                }
                catch (Exception e) when (e is TimeoutException || e is ObjectDisposedException || e is OperationCanceledException)
                {
                    if (closeReason != CloseReasons.Abort)
                    {
                        throw;
                    }
                    TraceWarning($"Failed to close the output session to [{_remoteSessionUri}]. {e}", GetType());
                }
            }
            base.OnClose(timeoutTimer.RemainingTime, closeReason);
            DisposeHelper.DisposeIfNotNull(_queueReader);
            _remoteSessionUri = null;
        }

        public override void Send(Message message, TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (ConcurrentOperationManager.TrackOperation())
            {
                if (InputSessionClosingRequestReceived)
                {
                    var queueStatus = _queueReader.QueryQueue(timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
                    //Fault the channel if there are no more messages to read.
                    if (queueStatus.MessageCount == 0)
                    {
                        OnFaulted();
                    }
                    throw new CommunicationException("The remote session was closed.");
                }
                message.Headers.To = _remoteSessionUri;
                message.Headers.From = LocalAddress;
                QueueWriter.Enqueue(Binding.Exchange, _remoteSessionUri.QueueName, message, Manager.BufferManager, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
            }
        }

        public override Message Receive(TimeSpan timeout)
        {
            var msg = base.Receive(timeout);

            //if the server closed the session.
            if ((msg == null) && InputSessionClosingRequestReceived)
            {
                OnFaulted();
                throw new CommunicationException("The remote channel closed the session.");
            }
            return msg;
        }
    }
}
