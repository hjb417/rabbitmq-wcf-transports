﻿/*
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
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Xml;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;
using RabbitMQ.Client;
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
        private string _abortTopic;
        private string _abortTopicExchange;

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
                _abortTopic = null;
                _abortTopicExchange = null;
                createSessionReqMsg.Headers.ReplyTo = LocalAddress;
                createSessionReqMsg.Headers.From = LocalAddress;
                createSessionReqMsg.Headers.To = RemoteAddress.Uri;
                createSessionReqMsg.Headers.MessageId = new UniqueId();

                var connFactory = Binding.CreateConnectionFactory(RemoteAddress.Uri.Host, RemoteAddress.Uri.Port);

                try
                {
                    var setup = new RabbitMQReaderSetup
                    {
                        CancelToken = ConcurrentOperationManager.Token,
                        ConnectionFactory = connFactory,
                        DeleteQueueOnClose = Binding.DeleteOnClose,
                        Exchange = Binding.Exchange,
                        IsDurable = Binding.IsDurable,
                        MaxPriority = null,
                        Options = Binding.ReaderOptions,
                        QueueName = localUri.QueueName,
                        QueueTimeToLive = Binding.ReplyQueueTimeToLive,
                        Timeout = timer.RemainingTime,
                    };
                    setup.QueueArguments = new Dictionary<string, object>();
                    setup.QueueArguments.Add(TaskQueueReaderQueueArguments.IsTaskInputQueue, false);
                    setup.QueueArguments.Add(TaskQueueReaderQueueArguments.Scheme, Constants.Scheme);

                    _queueReader = Binding.QueueReaderWriterFactory.CreateReader(setup);
                    QueueWriter.Enqueue(Binding.Exchange, _remoteAddress.QueueName, createSessionReqMsg, _bufferMgr, Binding, MessageEncoderFactory, timer.RemainingTime, timer.RemainingTime, ConcurrentOperationManager.Token);
                    using (var msg = _queueReader.Dequeue(Binding, MessageEncoderFactory, timer.RemainingTime, ConcurrentOperationManager.Token))
                    {
                        var response = msg.GetBody<CreateSessionResponse>();
                        _abortTopic = response.AbortTopic;
                        _abortTopicExchange = response.AbortTopicExchange;
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
            if (CloseSessionRequestReceived)
            {
                using (var msg = Message.CreateMessage(MessageVersion.Default, Actions.CloseSessionResponse, new CloseSessionResponse()))
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
                        if (e is RemoteQueueDoesNotExistException)
                        {
                            rethrow = false;
                        }
                        TraceWarning($"Failed to notify the endpoint [{_remoteSessionUri}] that the queue is closing. {e}", GetType());
                        if (rethrow)
                        {
                            throw;
                        }
                    }
                }
            }
            else
            {
                try
                {
                    //when aborting and timeout=zero, set timeout to 30s to try to notify server that client is aborting.
                    var closeSessionTimeout = (closeReason == CloseReasons.Abort)
                        ? TimeoutTimer.StartNew(TimeSpanHelper.Max(timeoutTimer.RemainingTime, TimeSpan.FromSeconds(30)))
                        : TimeoutTimer.StartNew(timeoutTimer.RemainingTime);
                    if (closeReason == CloseReasons.Abort)
                    {
                        QueueWriter.Publish(_abortTopicExchange, _abortTopic, new MemoryStream(), closeSessionTimeout.RemainingTime, ConcurrentOperationManager.Token);
                    }
                    CloseOutputSession(closeSessionTimeout.RemainingTime, false);
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
                if (CloseSessionRequestReceived)
                {
                    //TODO: Shouldn't we just always call OnFaulted and throw?
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
            if ((msg == null) && CloseSessionRequestReceived)
            {
                OnFaulted();
                throw new CommunicationException("The remote channel closed the session.");
            }
            return msg;
        }
    }
}
