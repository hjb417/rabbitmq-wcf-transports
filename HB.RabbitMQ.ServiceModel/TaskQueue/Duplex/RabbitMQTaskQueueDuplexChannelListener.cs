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
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;
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
            var connFactory = Binding.CreateConnectionFactory(_listenUri.Host, _listenUri.Port);

            var readerSetup = new RabbitMQReaderSetup
            {
                CancelToken = ConcurrentOperationManager.Token,
                ConnectionFactory = connFactory,
                DeleteQueueOnClose = Binding.DeleteOnClose,
                Exchange = Binding.Exchange,
                IsDurable = Binding.IsDurable,
                MaxPriority = null,
                Options = Binding.ReaderOptions,
                QueueName = _listenUri.QueueName,
                QueueTimeToLive = Binding.TimeToLive,
                Timeout = timeoutTimer.RemainingTime,
            };

            _reader = Binding.QueueReaderWriterFactory.CreateReader(readerSetup);

            var writerSetup = new RabbitMQWriterSetup
            {
                CancelToken = ConcurrentOperationManager.Token,
                ConnectionFactory = connFactory,
                Options = Binding.WriterOptions,
                Timeout = timeoutTimer.RemainingTime,
            };

            _writer = Binding.QueueReaderWriterFactory.CreateWriter(writerSetup);
        }

        protected override TChannel OnAcceptChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            if (State != CommunicationState.Opened)
            {
                return null;
            }
            var localUri = RabbitMQTaskQueueUri.Create(Uri.Host, Uri.Port, "s" + Guid.NewGuid().ToString("N"));
            var localAddress = new EndpointAddress(localUri);
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
                    RabbitMQTaskQueueAppDomainProtocolHandler.ReportMessageReceived(_listenUri);
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
                        var connFactory = Binding.CreateConnectionFactory(clientUri.Host, clientUri.Port);

                        var setup = new RabbitMQReaderSetup
                        {
                            CancelToken = ConcurrentOperationManager.Token,
                            ConnectionFactory = connFactory,
                            DeleteQueueOnClose = true,
                            Exchange = Binding.Exchange,
                            IsDurable = false,
                            MaxPriority = null,
                            Options = Binding.ReaderOptions,
                            QueueName = localUri.QueueName,
                            QueueTimeToLive = Binding.TimeToLive,
                            Timeout = timer.RemainingTime,
                        };

                        reader = Binding.QueueReaderWriterFactory.CreateReader(setup);
                        _writer.Enqueue(Binding.Exchange, clientUri.QueueName, createSessionRespMsg, BufferManager, Binding, _msgEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
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
