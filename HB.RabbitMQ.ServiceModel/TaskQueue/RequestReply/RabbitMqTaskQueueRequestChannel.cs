using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.Throttling;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueRequestChannel : RabbitMQTaskQueueChannelBase, IRequestChannel
    {
        private IRabbitMQReader _queueReader;
        private readonly Func<Message, TimeSpan, Message> _requestFunc;
        private readonly BufferManager _bufferMgr;
        private readonly RabbitMQTaskQueueUri _localAddress;

        public RabbitMQTaskQueueRequestChannel(
            BindingContext context,
            ChannelManagerBase channelManager,
            EndpointAddress remoteAddress,
            Uri via,
            BufferManager bufferManger,
            RabbitMQTaskQueueBinding binding
            )
            : base(context, channelManager, binding)
        {
            MethodInvocationTrace.Write();
            _localAddress = RabbitMQTaskQueueUri.Create("r" + Guid.NewGuid().ToString("N"), Constants.DefaultExchange, true, true, binding.QueueTimeToLive);
            _bufferMgr = bufferManger;
            _requestFunc = Request;
            RemoteAddress = remoteAddress;
            Via = via;
            RemoteUri = new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
            LocalAddress = new EndpointAddress(_localAddress);
        }


        public RabbitMQTaskQueueUri RemoteUri { get; private set; }
        public EndpointAddress RemoteAddress { get; private set; }
        public EndpointAddress LocalAddress { get; private set; }

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
            if (RemoteUri.IsDurable)
            {
                Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, RemoteUri.Exchange, RemoteUri.QueueName, RemoteUri.IsDurable, false, RemoteUri.TimeToLive, NoOpDequeueThrottler.Instance, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions).Dispose();
            }
            _queueReader = Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, _localAddress.Exchange, _localAddress.QueueName, _localAddress.IsDurable, _localAddress.DeleteOnClose, _localAddress.TimeToLive, NoOpDequeueThrottler.Instance, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions);
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
                QueueWriter.Enqueue(RemoteUri.Exchange, RemoteUri.QueueName, message, _bufferMgr, Binding, MessageEncoderFactory, TimeSpan.MaxValue, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
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