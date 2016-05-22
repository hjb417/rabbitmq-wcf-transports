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

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal sealed class RabbitMQTaskQueueDuplexChannelFactory<TChannel> : RabbitMQTaskQueueChannelFactoryBase<TChannel>
        where TChannel : IChannel
    {
        public RabbitMQTaskQueueDuplexChannelFactory(BindingContext context, RabbitMQTransportBindingElement bingingElement, RabbitMQTaskQueueBinding binding)
            : base(context, bingingElement, binding)
        {
            MethodInvocationTrace.Write();
        }

        protected override TChannel OnCreateChannel(EndpointAddress remoteAddress, Uri via)
        {
            MethodInvocationTrace.Write();
            var localAddress = RabbitMQTaskQueueUri.Create(remoteAddress.Uri.Host, remoteAddress.Uri.Port, "c" + Guid.NewGuid().ToString("N"));
            return (TChannel)(object)new RabbitMQTaskQueueClientDuplexChannel<TChannel>(Context, this, Binding, new EndpointAddress(localAddress), remoteAddress, BufferManager);
        }
    }
}
