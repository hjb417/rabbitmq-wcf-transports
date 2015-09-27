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