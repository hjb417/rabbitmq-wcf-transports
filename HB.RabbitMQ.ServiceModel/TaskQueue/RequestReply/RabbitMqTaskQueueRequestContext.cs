using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueRequestContext : RequestContext
    {
        private readonly Message _rqMsg;
        private readonly RabbitMQTaskQueueBinding _binding;
        private readonly Action<Message, TimeSpan> _reply;
        private readonly IRabbitMQWriter _queueWriter;
        private readonly ConcurrentOperationManager _opMgr;
        private readonly EndpointAddress _replyToAddress;
        private readonly MessageEncoderFactory _msgEncoderFactory;
        private readonly BufferManager _bufferMgr;

        public RabbitMQTaskQueueRequestContext(Message requestMessage, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, MessageEncoderFactory msgEncoderFactory, BufferManager bufferManager, IRabbitMQWriter queueWriter)
        {
            _opMgr = new ConcurrentOperationManager(GetType().FullName);
            _bufferMgr = bufferManager;
            _msgEncoderFactory = msgEncoderFactory;
            _replyToAddress = localAddress;
            _binding = binding;
            _rqMsg = requestMessage;
            _reply = Reply;
            _queueWriter = queueWriter;
        }

        public override Message RequestMessage { get { return _rqMsg; } }

        public override void Abort()
        {
            MethodInvocationTrace.Write();
            Close(TimeSpan.Zero);
        }

        public override IAsyncResult BeginReply(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _reply.BeginInvoke(message, timeout, callback, state);
        }

        public override IAsyncResult BeginReply(Message message, AsyncCallback callback, object state)
        {
            return BeginReply(message, _binding.SendTimeout, callback, state);
        }

        public override void EndReply(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _reply.EndInvoke(result);
        }

        public override void Close(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            _opMgr.Dispose();
            _queueWriter.Dispose();
        }

        public override void Close()
        {
            Close(_binding.CloseTimeout);
        }

        public override void Reply(Message message, TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            if (message == null)
            {
                return;
            }
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (_opMgr.TrackOperation())
            {
                var remoteAddress = new RabbitMQTaskQueueUri(RequestMessage.Headers.ReplyTo.Uri.ToString());
                message.Headers.From = _replyToAddress;
                message.Headers.ReplyTo = _replyToAddress;
                message.Headers.MessageId = new UniqueId();
                _queueWriter.Enqueue(remoteAddress.Exchange, remoteAddress.QueueName, message, _bufferMgr, _binding, _msgEncoderFactory, TimeSpan.MaxValue, timeoutTimer.RemainingTime, _opMgr.Token);
            }
        }

        public override void Reply(Message message)
        {
            Reply(message, _binding.SendTimeout);
        }
    }
}