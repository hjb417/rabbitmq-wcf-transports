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
        private readonly ulong _deliveryTag;

        public RabbitMQTaskQueueRequestContext(Message requestMessage, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress, MessageEncoderFactory msgEncoderFactory, BufferManager bufferManager, IRabbitMQWriter queueWriter, ulong deliveryTag)
        {
            _opMgr = new ConcurrentOperationManager(GetType().FullName);
            _deliveryTag = deliveryTag;
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
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (_opMgr.TrackOperation())
            {
                if (message == null)
                {
                    return;
                }
                _queueWriter.AcknowledgeMessage(_deliveryTag, timeoutTimer.RemainingTime, _opMgr.Token);
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
