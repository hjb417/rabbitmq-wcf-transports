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
            var queueName = "c" + Guid.NewGuid().ToString("N");
            var localAddress = RabbitMQTaskQueueUri.Create(queueName, Constants.DefaultExchange, true, true, Binding.QueueTimeToLive);
            return (TChannel)(object)new RabbitMQTaskQueueClientDuplexChannel<TChannel>(Context, this, Binding, new EndpointAddress(localAddress), remoteAddress, BufferManager);
        }
    }
}