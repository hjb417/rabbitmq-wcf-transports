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
            try
            {
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
            catch(OperationCanceledException)
            {
                if(State != CommunicationState.Opened)
                {
                    return null;
                }
                throw;
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
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            base.OnOpen(timeoutTimer.RemainingTime);
            var url = new RabbitMQTaskQueueUri(Uri.ToString());
            //create the queue
            using(Binding.QueueReaderWriterFactory.CreateReader(Binding.ConnectionFactory, url.Exchange, url.QueueName, url.IsDurable, url.DeleteOnClose, url.TimeToLive, timeoutTimer.RemainingTime, ConcurrentOperationManager.Token, Binding.ReaderOptions, url.MaxPriority))
            {
                _inputChannels = new ConcurrentQueue<RabbitMQTaskQueueReplyChannel>();
                _inputChannels.Enqueue(CreateInputChannel());
                _inputChannelsBuffer = new BlockingCollection<RabbitMQTaskQueueReplyChannel>(_inputChannels);
            }
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