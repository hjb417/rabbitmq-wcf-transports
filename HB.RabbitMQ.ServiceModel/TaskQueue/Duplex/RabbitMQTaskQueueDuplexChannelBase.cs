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
using System.ServiceModel;
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal abstract class RabbitMQTaskQueueDuplexChannelBase : RabbitMQTaskQueueChannelBase, IDuplexSessionChannel, IDuplexSession
    {
        private readonly Func<TimeSpan, Message> _receiveFunc;
        private readonly Func<TimeSpan, bool> _waitForMsgFunc;
        private readonly Action<Message, TimeSpan> _sendFunc;
        private readonly TryReceiveDelegate _tryReceiveMethod;
        private readonly EndpointAddress _remoteAddress;
        private readonly RabbitMQTaskQueueUri _remoteUri;
        private readonly Action<TimeSpan> _closeOutputSessionFunc;
        private volatile bool _closeSessionRequestReceived;

        protected RabbitMQTaskQueueDuplexChannelBase(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress remoteAddress, EndpointAddress localAddress)
            : base(context, channelManager, binding, localAddress)
        {
            MethodInvocationTrace.Write();
            _tryReceiveMethod = TryReceive;
            _receiveFunc = Receive;
            _waitForMsgFunc = WaitForMessage;
            _sendFunc = Send;
            _remoteAddress = remoteAddress;
            _closeOutputSessionFunc = CloseOutputSession;
            _remoteUri = (remoteAddress.Uri == EndpointAddress.AnonymousUri) ? null : new RabbitMQTaskQueueUri(remoteAddress.Uri.ToString());
        }


        public IDuplexSession Session { get { return this; } }
        public string Id { get { return LocalAddress.Uri.ToString(); } }
        public virtual Uri Via { get { return null; } }
        protected abstract IRabbitMQReader QueueReader { get; }
        public EndpointAddress RemoteAddress { get { return _remoteAddress; } }

        protected bool CloseSessionRequestReceived
        {
            get { return _closeSessionRequestReceived; }
            set
            {
                _closeSessionRequestReceived = value;
                var reader = QueueReader;
                if(value && (reader != null))
                {
                    reader.SoftClose();
                }
            }
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            CloseSessionRequestReceived = false;
            base.OnOpen(timeoutTimer.RemainingTime);
        }

        public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _receiveFunc.BeginInvoke(timeout, callback, state);
        }

        public IAsyncResult BeginReceive(AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return BeginReceive(DefaultReceiveTimeout, callback, state);
        }

        public Message EndReceive(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _receiveFunc.EndInvoke(result);
        }

        public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            Message message;
            return _tryReceiveMethod.BeginInvoke(timeout, out message, callback, state);
        }

        public bool EndTryReceive(IAsyncResult result, out Message message)
        {
            MethodInvocationTrace.Write();
            return _tryReceiveMethod.EndInvoke(out message, result);
        }

        public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _waitForMsgFunc.BeginInvoke(timeout, callback, state);
        }

        public bool EndWaitForMessage(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _waitForMsgFunc.EndInvoke(result);
        }

        public Message Receive()
        {
            return Receive(DefaultReceiveTimeout);
        }

        public bool TryReceive(TimeSpan timeout, out Message message)
        {
            MethodInvocationTrace.Write();
            message = null;
            try
            {
                message = Receive(timeout);
                if(message != null)
                {
                    return true;
                }
                if(CloseSessionRequestReceived)
                {
                    return true;
                }
                if(State != CommunicationState.Opened)
                {
                    return true;
                }
                return false;
            }
            catch (ObjectDisposedException)
            {
                if(State != CommunicationState.Opened)
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

        public bool WaitForMessage(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            using (ConcurrentOperationManager.TrackOperation())
            {
                return QueueReader.WaitForMessage(timeout, ConcurrentOperationManager.Token);
            }
        }

        public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _sendFunc.BeginInvoke(message, timeout, callback, state);
        }

        public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
        {
            return BeginSend(message, DefaultSendTimeout, callback, state);
        }

        public void EndSend(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _sendFunc.EndInvoke(result);
        }

        public abstract void Send(Message message, TimeSpan timeout);

        public virtual Message Receive(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            using (ConcurrentOperationManager.TrackOperation())
            {
                while (true)
                {
                    switch(State)
                    {
                        case CommunicationState.Opened:
                        case CommunicationState.Closing:
                            break;
                        default:
                            return null;
                    }
                    //if request from other endpoint received indicating that it's closing,
                    //then close the reader when the message count drops to zero.
                    if (CloseSessionRequestReceived)
                    {
                        var queueStatus = QueueReader.QueryQueue(timer.RemainingTime, ConcurrentOperationManager.Token);
                        if (queueStatus.MessageCount == 0)
                        {
                            QueueReader.SoftClose();
                            return null;
                        }
                    }
                    var msg = QueueReader.Dequeue(Binding, MessageEncoderFactory, timer.RemainingTime, ConcurrentOperationManager.Token);
                    if(msg == null)
                    {
                        continue;
                    }
                    //RabbitMQTaskQueueAppDomainProtocolHandler.ReportMessageReceived(ListenerChannelSetup);
                    msg.Headers.To = BindingContext.ListenUriBaseAddress;
                    if(msg.Headers.Action == Actions.CloseSessionRequest)
                    {
                        msg.Close();
                        CloseSessionRequestReceived = true;
                        msg = null;
                    }
                    return msg;
                }
            }
        }

        public void Send(Message message)
        {
            Send(message, DefaultSendTimeout);
        }

        public IAsyncResult BeginCloseOutputSession(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _closeOutputSessionFunc.BeginInvoke(timeout, callback, state);
        }

        public IAsyncResult BeginCloseOutputSession(AsyncCallback callback, object state)
        {
            return BeginCloseOutputSession(DefaultCloseTimeout, callback, state);
        }

        public void EndCloseOutputSession(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _closeOutputSessionFunc.EndInvoke(result);
        }

        public void CloseOutputSession(TimeSpan timeout)
        {
            CloseOutputSession(timeout, false);
        }

        protected void CloseOutputSession(TimeSpan timeout, bool waitForResponse)
        {
            MethodInvocationTrace.Write();
            if (ConcurrentOperationManager == null)
            {
                //the session was never opened.
                return;
            }
            if (CloseSessionRequestReceived)
            {
                return;
            }
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (ConcurrentOperationManager.TrackOperation())
            using (var msg = Message.CreateMessage(MessageVersion.Default, Actions.CloseSessionRequest, new CloseSessionRequest()))
            {
                Send(msg, timeoutTimer.RemainingTime);
                while(waitForResponse && !CloseSessionRequestReceived)
                {
                    using (var resp = Receive(timeoutTimer.RemainingTime))
                    {
                        if (resp == null)
                        {
                            continue;
                        }
                        if(resp.Headers.Action == Actions.CloseSessionResponse)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public void CloseOutputSession()
        {
            CloseOutputSession(DefaultCloseTimeout);
        }
    }
}
