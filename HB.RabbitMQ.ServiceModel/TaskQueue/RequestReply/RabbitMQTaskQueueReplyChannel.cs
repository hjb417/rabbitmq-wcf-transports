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
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueReplyChannel : RabbitMQTaskQueueChannelBase, IReplyChannel
    {
        private IRabbitMQReader _queueReader;
        private readonly Func<TimeSpan, RequestContext> _receiveRequest;
        private readonly Func<TimeSpan, bool> _waitForRequest;
        private readonly BufferManager _bufferMgr;
        private readonly TryReceiveRequestDelegate _tryReceiveRequest;

        public RabbitMQTaskQueueReplyChannel(BindingContext context, ChannelManagerBase channelManager, EndpointAddress localAddress, BufferManager bufferManger, RabbitMQTaskQueueBinding binding)
            : base(context, channelManager, binding, localAddress)
        {
            _waitForRequest = WaitForRequest;
            _bufferMgr = bufferManger;
            _receiveRequest = ReceiveRequest;
            _tryReceiveRequest = TryReceiveRequest;
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timeoutTimer.RemainingTime);

            var url = new RabbitMQTaskQueueUri(LocalAddress.Uri.ToString());
            var connFactory = Binding.CreateConnectionFactory(url.Host, url.Port);

            var setup = new RabbitMQReaderSetup
            {
                CancelToken = ConcurrentOperationManager.Token,
                ConnectionFactory = connFactory,
                DeleteQueueOnClose = Binding.DeleteOnClose,
                Exchange = Binding.Exchange,
                IsDurable = Binding.IsDurable,
                MaxPriority = Binding.MaxPriority,
                Options = Binding.ReaderOptions,
                QueueName = url.QueueName,
                QueueTimeToLive = Binding.ReplyQueueTimeToLive,
                Timeout = timeoutTimer.RemainingTime,
            };
            setup.QueueArguments = new Dictionary<string, object>();
            setup.QueueArguments.Add(TaskQueueReaderQueueArguments.IsTaskInputQueue, false);
            setup.QueueArguments.Add(TaskQueueReaderQueueArguments.Scheme, Constants.Scheme);

            _queueReader = Binding.QueueReaderWriterFactory.CreateReader(setup);
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
                DisposeHelper.DisposeIfNotNull(_queueReader);
            }
        }

        public IAsyncResult BeginReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _receiveRequest.BeginInvoke(timeout, callback, state);
        }

        public IAsyncResult BeginReceiveRequest(AsyncCallback callback, object state)
        {
            return BeginReceiveRequest(DefaultReceiveTimeout, callback, state);
        }

        public IAsyncResult BeginTryReceiveRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            RequestContext context;
            return _tryReceiveRequest.BeginInvoke(timeout, out context, callback, state);
        }

        public bool EndTryReceiveRequest(IAsyncResult result, out RequestContext context)
        {
            MethodInvocationTrace.Write();
            return _tryReceiveRequest.EndInvoke(out context, result);
        }

        public IAsyncResult BeginWaitForRequest(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _waitForRequest.BeginInvoke(timeout, callback, state);
        }

        public bool EndWaitForRequest(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _waitForRequest.EndInvoke(result);
        }

        public RequestContext EndReceiveRequest(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _receiveRequest.EndInvoke(result);
        }

        public RequestContext ReceiveRequest(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            IRabbitMQWriter queueWriter = null;
            using (ConcurrentOperationManager.TrackOperation())
            {
                try
                {
                    var connFactory = Binding.CreateConnectionFactory(LocalAddress.Uri.Host, LocalAddress.Uri.Port);
                    var setup = new RabbitMQWriterSetup
                    {
                        CancelToken = ConcurrentOperationManager.Token,
                        ConnectionFactory = connFactory,
                        Options = Binding.WriterOptions,
                        Timeout = timeoutTimer.RemainingTime,
                    };

                    queueWriter = Binding.QueueReaderWriterFactory.CreateWriter(setup);
                    ulong deliveryTag;
                    var request = _queueReader.Dequeue(Binding, MessageEncoderFactory, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, out deliveryTag);
                    RabbitMQTaskQueueAppDomainProtocolHandler.ReportMessageReceived(request.Headers.To);
                    if (Binding.MessageConfirmationMode == MessageConfirmationModes.AfterReceive)
                    {
                        _queueReader.AcknowledgeMessage(deliveryTag, TimeSpan.MaxValue, CancellationToken.None);
                    }
                    return new RabbitMQTaskQueueRequestContext(request, Binding, LocalAddress, MessageEncoderFactory, _bufferMgr, queueWriter, deliveryTag, _queueReader, ConcurrentOperationManager.TrackOperation());
                }
                catch (OperationCanceledException)
                {
                    DisposeHelper.DisposeIfNotNull(queueWriter);
                    return null;
                }
                catch
                {
                    DisposeHelper.DisposeIfNotNull(queueWriter);
                    throw;
                }
            }
        }

        public RequestContext ReceiveRequest()
        {
            return ReceiveRequest(DefaultReceiveTimeout);
        }

        public bool TryReceiveRequest(TimeSpan timeout, out RequestContext context)
        {
            MethodInvocationTrace.Write();
            context = null;
            if (State != CommunicationState.Opened)
            {
                //HACK: even in closed state, WCF is always calling BeginTryReceive
                // https://social.msdn.microsoft.com/Forums/vstudio/en-US/239647bb-737e-4476-a7f9-366d37115428/what-is-the-proper-way-to-close-down-a-custom-transport-channel?forum=wcf
                return true;
            }
            try
            {
                context = ReceiveRequest(timeout);
                return context != null;
            }
            catch (ObjectDisposedException)
            {
                if (State != CommunicationState.Opened)
                {
                    return true;
                }
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        public bool WaitForRequest(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            using (ConcurrentOperationManager.TrackOperation())
            {
                return _queueReader.WaitForMessage(timeout, ConcurrentOperationManager.Token);
            }
        }
    }
}
