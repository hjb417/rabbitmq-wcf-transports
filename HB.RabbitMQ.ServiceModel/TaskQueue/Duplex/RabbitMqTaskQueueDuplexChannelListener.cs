using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueDuplexChannelListener<TChannel> : RabbitMQTaskQueueChannelListenerBase<TChannel>
        where TChannel : class, IChannel
    {
        private IRabbitMQReader _reader;
        private IRabbitMQWriter _writer;
        private readonly MessageEncoderFactory _msgEncoderFactory;
        private readonly RabbitMQTaskQueueUri _listenUri;

        public RabbitMQTaskQueueDuplexChannelListener(BindingContext context, RabbitMQTaskQueueBinding binding)
            : base(context, binding)
        {
            MethodInvocationTrace.Write();
            _msgEncoderFactory = context.GetMessageEncoderFactory();
            _listenUri = new RabbitMQTaskQueueUri(context.ListenUriBaseAddress.ToString());
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timeoutTimer.RemainingTime);
            var listenUri = new RabbitMQTaskQueueUri(Context.ListenUriBaseAddress.ToString());
            var throttler = Binding.DequeueThrottlerFactory.Create(listenUri.Exchange, listenUri.QueueName);
            _reader = Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, listenUri.Exchange, listenUri.QueueName, listenUri.IsDurable, listenUri.DeleteOnClose, listenUri.TimeToLive, throttler, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
            _writer = Binding.QueueReaderWriterFactory.CreateWriter(Binding.ConnectionFactory, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
        }

        protected override TChannel OnAcceptChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            if (State != CommunicationState.Opened)
            {
                return null;
            }
            var queueName = "s" + Guid.NewGuid().ToString("N");
            var localAddress = new EndpointAddress(RabbitMQTaskQueueUri.Create(queueName, _listenUri.Exchange, true, true, Binding.QueueTimeToLive));
            var createSessionResp = new CreateSessionResponse();
            Message msg = null;
            try
            {
                using (ConcurrentOperationManager.TrackOperation())
                using (var createSessionRespMsg = Message.CreateMessage(MessageVersion.Default, Actions.CreateSessionResponse, createSessionResp))
                {

                    try
                    {
                        msg = _reader.Dequeue(Binding, _msgEncoderFactory, timer.RemainingTime, ConcurrentOperationManager.Token);
                    }
                    catch
                    {
                        Fault();
                        throw;
                    }
                    createSessionRespMsg.Headers.ReplyTo = localAddress;
                    createSessionRespMsg.Headers.From = new EndpointAddress(_listenUri);
                    createSessionRespMsg.Headers.To = msg.Headers.ReplyTo.Uri;
                    createSessionRespMsg.Headers.MessageId = new UniqueId();
                    createSessionRespMsg.Headers.RelatesTo = msg.Headers.MessageId;

                    var clientUri = new RabbitMQTaskQueueUri(msg.Headers.ReplyTo.Uri.ToString());
                    var createSessionReq = msg.GetBody<CreateSessionRequest>();
                    IRabbitMQReader reader = null;
                    try
                    {
                        var throttler = Binding.DequeueThrottlerFactory.Create(_listenUri.Exchange, queueName);
                        reader = Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, _listenUri.Exchange, queueName, false, true, _listenUri.TimeToLive, throttler, timer.RemainingTime, ConcurrentOperationManager.Token);
                        _writer.Enqueue(clientUri.Exchange, clientUri.QueueName, createSessionRespMsg, BufferManager, Binding, _msgEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
                        return (TChannel)(object)new RabbitMQTaskQueueServerDuplexChannel(Context, this, Binding, localAddress, msg.Headers.ReplyTo, BufferManager, reader);
                    }
                    catch
                    {
                        DisposeHelper.DisposeIfNotNull(reader);
                        throw;
                    }
                }
            }
            finally
            {
                DisposeHelper.DisposeIfNotNull(msg);
            }
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            using (ConcurrentOperationManager.TrackOperation())
            {
                return _reader.WaitForMessage(timeout, ConcurrentOperationManager.Token);
            }
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            base.OnClose(timeout, closeReason);
            DisposeHelper.DisposeIfNotNull(_reader);
            DisposeHelper.DisposeIfNotNull(_writer);
        }
    }
}