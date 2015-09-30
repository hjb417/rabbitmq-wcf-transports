using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueClientDuplexChannel<TChannel> : RabbitMQTaskQueueDuplexChannelBase
        where TChannel : IChannel
    {
        private readonly EndpointAddress _localAddress;
        private readonly RabbitMQTaskQueueUri _remoteAddress;
        private IRabbitMQReader _queueReader;
        private readonly BufferManager _bufferMgr;
        private RabbitMQTaskQueueUri _remoteSessionUri;

        public RabbitMQTaskQueueClientDuplexChannel(BindingContext context, RabbitMQTaskQueueDuplexChannelFactory<TChannel> channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, EndpointAddress remoteAddress, BufferManager bufferManager)
            : base(context, channelManager, binding, remoteAddress)
        {
            MethodInvocationTrace.Write();
            _bufferMgr = bufferManager;
            _remoteAddress = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
            _localAddress = localAddress;
        }

        private new RabbitMQTaskQueueDuplexChannelFactory<TChannel> Manager { get { return (RabbitMQTaskQueueDuplexChannelFactory<TChannel>)base.Manager; } }
        public override EndpointAddress LocalAddress { get { return _localAddress; } }
        protected override IRabbitMQReader QueueReader { get { return _queueReader; } }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timer.RemainingTime);

            var localUri = new RabbitMQTaskQueueUri(_localAddress.Uri.ToString());
            using (ConcurrentOperationManager.TrackOperation())
            using (var createSessionReqMsg = Message.CreateMessage(MessageVersion.Default, Actions.CreateSessionRequest, new CreateSessionRequest()))
            {
                createSessionReqMsg.Headers.ReplyTo = LocalAddress;
                createSessionReqMsg.Headers.From = LocalAddress;
                createSessionReqMsg.Headers.To = RemoteAddress.Uri;
                createSessionReqMsg.Headers.MessageId = new UniqueId();

                var msgThrottle = Binding.DequeueThrottlerFactory.Create(localUri.Exchange, localUri.QueueName);
                try
                {
                    _queueReader = Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, localUri.Exchange, localUri.QueueName, localUri.IsDurable, localUri.DeleteOnClose, localUri.TimeToLive, msgThrottle, timer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions);
                    QueueWriter.Enqueue(_remoteAddress.Exchange, _remoteAddress.QueueName, createSessionReqMsg, _bufferMgr, Binding, MessageEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
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
                catch (TimeoutException e)
                {
                    if (closeReason != CloseReasons.Abort)
                    {
                        throw;
                    }
                    Trace.TraceWarning("[{2}] Failed to close the output session to [{0}]. {1}", _remoteSessionUri, e, GetType());
                }
                catch (ObjectDisposedException e)
                {
                    if (closeReason != CloseReasons.Abort)
                    {
                        throw;
                    }
                    Trace.TraceWarning("[{2}] Failed to close the output session to [{0}]. {1}", _remoteSessionUri, e, GetType());
                }
                catch (OperationCanceledException e)
                {
                    if (closeReason != CloseReasons.Abort)
                    {
                        throw;
                    }
                    Trace.TraceWarning("[{2}] Failed to close the output session to [{0}]. {1}", _remoteSessionUri, e, GetType());
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
                QueueWriter.Enqueue(_remoteSessionUri.Exchange, _remoteSessionUri.QueueName, message, Manager.BufferManager, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
            }
        }

        public override Message Receive(TimeSpan timeout)
        {
            var msg = base.Receive(timeout);

            //if the server closed the session.
            if((msg == null) && InputSessionClosingRequestReceived)
            {
                OnFaulted();
                throw new CommunicationException("The remote channel closed the session.");
            }
            return msg;
        }
    }
}