using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueReplyChannelListener : RabbitMQTaskQueueChannelListenerBase<IReplyChannel>
    {
        private BlockingCollection<RabbitMQTaskQueueReplyChannel> _inputChannelsBuffer;
        private ConcurrentQueue<RabbitMQTaskQueueReplyChannel> _inputChannels;

        public RabbitMQTaskQueueReplyChannelListener(BindingContext context, RabbitMQTaskQueueBinding binding)
            : base(context, binding)
        {
            MethodInvocationTrace.Write();
        }

        protected override IReplyChannel OnAcceptChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            RabbitMQTaskQueueReplyChannel channel;
            if (State != CommunicationState.Opened)
            {
                return null;
            }
            using (ConcurrentOperationManager.TrackOperation())
            {
                var gotChannel = _inputChannelsBuffer.TryTake(out channel, timeout, ConcurrentOperationManager.Token);
                if (!gotChannel)
                {
                    throw new TimeoutException();
                }
                channel.Closed += OnInputChannelClosed;
                return channel;
            }
        }

        private void OnInputChannelClosed(object sender, EventArgs args)
        {
            MethodInvocationTrace.Write();
            if (State != CommunicationState.Opened)
            {
                return;
            }
            try
            {
                using (ConcurrentOperationManager.TrackOperation())
                {
                    _inputChannelsBuffer.Add(CreateInputChannel());
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("[{2}] Failed add a new input channel to listener on [{0}]. {1}", Uri, e, GetType());
            }
        }

        private RabbitMQTaskQueueReplyChannel CreateInputChannel()
        {
            MethodInvocationTrace.Write();
            return new RabbitMQTaskQueueReplyChannel
            (
                Context,
                this,
                new EndpointAddress(Uri),
                BufferManager,
                Binding
            );
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            base.OnOpen(timeout);
            _inputChannels = new ConcurrentQueue<RabbitMQTaskQueueReplyChannel>();
            _inputChannels.Enqueue(CreateInputChannel());
            _inputChannelsBuffer = new BlockingCollection<RabbitMQTaskQueueReplyChannel>(_inputChannels);
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            RabbitMQTaskQueueReplyChannel channel;
            try
            {
                using (ConcurrentOperationManager.TrackOperation())
                {
                    var gotChannel = _inputChannelsBuffer.TryTake(out channel, timeout, ConcurrentOperationManager.Token);
                    if (gotChannel)
                    {
                        _inputChannelsBuffer.TryAdd(channel);
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}