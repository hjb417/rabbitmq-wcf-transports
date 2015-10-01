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
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.TaskQueue.Duplex;
using HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed class RabbitMQTransportBindingElement : TransportBindingElement, IBindingDeliveryCapabilities
    {
        private readonly RabbitMQTaskQueueBinding _binding;
        private readonly Dictionary<Type, Func<BindingContext, IChannelFactory>> _channelFactoryFactotries = new Dictionary<Type, Func<BindingContext, IChannelFactory>>();
        private readonly Dictionary<Type, Func<BindingContext, IChannelListener>> _channelListenerFactories = new Dictionary<Type, Func<BindingContext, IChannelListener>>();

        public RabbitMQTransportBindingElement(RabbitMQTaskQueueBinding binding)
        {
            _binding = binding;

            _channelFactoryFactotries.Add(typeof(IRequestChannel), context => new RabbitMQTaskQueueRequestChannelFactory(context, this, _binding));
            _channelListenerFactories.Add(typeof(IReplyChannel), context => new RabbitMQTaskQueueReplyChannelListener(context, _binding));

            _channelFactoryFactotries.Add(typeof(IDuplexChannel), context => new RabbitMQTaskQueueDuplexChannelFactory<IDuplexChannel>(context, this, _binding));
            _channelListenerFactories.Add(typeof(IDuplexChannel), context => new RabbitMQTaskQueueDuplexChannelListener<IDuplexChannel>(context, _binding));

            _channelFactoryFactotries.Add(typeof(IDuplexSessionChannel), context => new RabbitMQTaskQueueDuplexChannelFactory<IDuplexSessionChannel>(context, this, _binding));
            _channelListenerFactories.Add(typeof(IDuplexSessionChannel), context => new RabbitMQTaskQueueDuplexChannelListener<IDuplexSessionChannel>(context, _binding));
        }

        private RabbitMQTransportBindingElement(RabbitMQTransportBindingElement other)
            : base(other)
        {
            _binding = other._binding;
            _channelFactoryFactotries = new Dictionary<Type, Func<BindingContext, IChannelFactory>>(other._channelFactoryFactotries);
            _channelListenerFactories = new Dictionary<Type, Func<BindingContext, IChannelListener>>(other._channelListenerFactories);
        }

        public override string Scheme { get { return Constants.Scheme; } }
        public bool AssuresOrderedDelivery { get { return false; } }
        public bool QueuedDelivery { get { return true; } }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            MethodInvocationTrace.Write<TChannel>();
            var factory = _channelFactoryFactotries[typeof(TChannel)](context);
            return (IChannelFactory<TChannel>)(object)factory;
        }

        public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
        {
            MethodInvocationTrace.Write<TChannel>();
            var listener = _channelListenerFactories[typeof(TChannel)](context);
            return (IChannelListener<TChannel>)(object)listener;
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            MethodInvocationTrace.Write<TChannel>();
            return _channelFactoryFactotries.ContainsKey(typeof(TChannel));
        }

        public override bool CanBuildChannelListener<TChannel>(BindingContext context)
        {
            MethodInvocationTrace.Write<TChannel>();
            return _channelListenerFactories.ContainsKey(typeof(TChannel));
        }

        public override BindingElement Clone()
        {
            return new RabbitMQTransportBindingElement(this);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            MethodInvocationTrace.Write<T>();
            if (typeof(T) == typeof(ISecurityCapabilities))
            {
                return null;
            }
            else if (typeof(T) == typeof(IBindingDeliveryCapabilities))
            {
                return (T)(object)this;
            }
            else
            {
                return base.GetProperty<T>(context);
            }
        }
    }
}