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
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;
using RabbitMQ.Client.Events;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueDuplexChannelListener<TChannel> : RabbitMQTaskQueueChannelListenerBase<TChannel>
        where TChannel : class, IChannel
    {
        private IRabbitMQReader _reader;
        private IRabbitMQWriter _writer;
        private readonly MessageEncoderFactory _msgEncoderFactory;
        private readonly RabbitMQTaskQueueUri _listenUri;
        private EventingBasicConsumer _clientClosedEvent;

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
                QueueTimeToLive = Binding.TaskQueueTimeToLive,
                Timeout = timeoutTimer.RemainingTime,
            };
            readerSetup.QueueArguments = new Dictionary<string, object>();
            readerSetup.QueueArguments.Add(TaskQueueReaderQueueArguments.IsTaskInputQueue, true);
            readerSetup.QueueArguments.Add(TaskQueueReaderQueueArguments.Scheme, Constants.Scheme);

            _reader = Binding.QueueReaderWriterFactory.CreateReader(readerSetup);
            _clientClosedEvent = _reader.CreateEventingBasicConsumer(Topics.RoutingKey, PredeclaredExchangeNames.Topic, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token);
            _clientClosedEvent.Received += OnClientClosed;

            var writerSetup = new RabbitMQWriterSetup
            {
                CancelToken = ConcurrentOperationManager.Token,
                ConnectionFactory = connFactory,
                Options = Binding.WriterOptions,
                Timeout = timeoutTimer.RemainingTime,
            };

            _writer = Binding.QueueReaderWriterFactory.CreateWriter(writerSetup);
        }

        private void OnClientClosed(object sender, BasicDeliverEventArgs e)
        {
            return;
        }

        protected override TChannel OnAcceptChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            var sessionId = Guid.NewGuid().ToString();
            var localUri = RabbitMQTaskQueueUri.Create(Uri.Host, Uri.Port, "s" + sessionId);
            var localAddress = new EndpointAddress(localUri);
            var abortTopic = Topics.ClientClose + "." + sessionId;
            var createSessionResp = new CreateSessionResponse
            {
                AbortTopic = abortTopic,
                AbortTopicExchange = PredeclaredExchangeNames.Topic
            };
            while (true)
            {
                if (State != CommunicationState.Opened)
                {
                    return null;
                }
                Message msg = null;
                try
                {
                    using (ConcurrentOperationManager.TrackOperation())
                    using (var createSessionRespMsg = Message.CreateMessage(MessageVersion.Default, Actions.CreateSessionResponse, createSessionResp))
                    {
                        msg = _reader.Dequeue(Binding, _msgEncoderFactory, timer.RemainingTime, ConcurrentOperationManager.Token);
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
                                QueueTimeToLive = Binding.ReplyQueueTimeToLive,
                                Timeout = timer.RemainingTime,
                            };
                            setup.QueueArguments = new Dictionary<string, object>();
                            setup.QueueArguments.Add(TaskQueueReaderQueueArguments.IsTaskInputQueue, false);
                            setup.QueueArguments.Add(TaskQueueReaderQueueArguments.Scheme, Constants.Scheme);

                            reader = Binding.QueueReaderWriterFactory.CreateReader(setup);
                            var channel = (TChannel)(object)new RabbitMQTaskQueueServerDuplexChannel(Context, this, Binding, localAddress, msg.Headers.ReplyTo, BufferManager, reader, abortTopic, _clientClosedEvent);
                            _writer.Enqueue(Binding.Exchange, clientUri.QueueName, createSessionRespMsg, BufferManager, Binding, _msgEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
                            return channel;
                        }
                        catch
                        {
                            DisposeHelper.DisposeIfNotNull(reader);
                            throw;
                        }
                    }
                }
                catch (Exception e)
                {
                    timer.ThrowIfNoTimeRemaining();
                    TraceError(e.ToString(), GetType());
                }
                finally
                {
                    DisposeHelper.DisposeIfNotNull(msg);
                }
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
            try
            {
                base.OnClose(timeout, closeReason);
            }
            finally
            {
                DisposeHelper.DisposeIfNotNull(_clientClosedEvent?.Model);
                DisposeHelper.DisposeIfNotNull(_reader);
                DisposeHelper.DisposeIfNotNull(_writer);
            }
        }
    }
}
