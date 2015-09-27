using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Messages;
using HB.RabbitMQ.ServiceModel.Throttling;

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

        protected RabbitMQTaskQueueDuplexChannelBase(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress remoteAddress)
            : base(context, channelManager, binding)
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
        public abstract EndpointAddress LocalAddress { get; }
        protected abstract IRabbitMQReader QueueReader { get; }
        public EndpointAddress RemoteAddress { get { return _remoteAddress; } }
        protected bool InputSessionClosingRequestReceived { get; private set; }

        protected bool CloseSessionRequestReceived
        {
            get { return _closeSessionRequestReceived; }
            private set
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
            InputSessionClosingRequestReceived = false;
            base.OnOpen(timeoutTimer.RemainingTime);
            if ((_remoteUri != null) && _remoteUri.IsDurable)
            {
                //create the queue if it doesn't already exist.
                Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, _remoteUri.Exchange, _remoteUri.QueueName, true, false, _remoteUri.TimeToLive, NoOpDequeueThrottler.Instance, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token).Dispose();
            }
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
                    if (State != CommunicationState.Opened)
                    {
                        return null;
                    }
                    //if request from other endpoint received indicating that it's closing,
                    //then close the reader when the message count drops to zero.
                    if (CloseSessionRequestReceived || InputSessionClosingRequestReceived)
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
                    msg.Headers.To = BindingContext.ListenUriBaseAddress;
                    if (msg.Headers.Action == Actions.CloseSessionRequest)
                    {
                        CloseSessionRequestReceived = true;
                        msg = null;
                    }
                    else if (msg.Headers.Action == Actions.InputSessionClosingRequest)
                    {
                        InputSessionClosingRequestReceived = true;
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
            MethodInvocationTrace.Write();
            if (ConcurrentOperationManager == null)
            {
                //the session was never opened.
                return;
            }
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (ConcurrentOperationManager.TrackOperation())
            using (var msg = Message.CreateMessage(MessageVersion.Default, Actions.CloseSessionRequest, new CloseSessionRequest()))
            {
                Send(msg, timeoutTimer.RemainingTime);
                //Receive(timeoutTimer.RemainingTime);
            }
        }

        public void CloseOutputSession()
        {
            CloseOutputSession(DefaultCloseTimeout);
        }
    }
}