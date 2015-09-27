using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply
{
    internal sealed class RabbitMQTaskQueueRequestChannelFactory : RabbitMQTaskQueueChannelFactoryBase<IRequestChannel>
    {
        public RabbitMQTaskQueueRequestChannelFactory(BindingContext context, RabbitMQTransportBindingElement bingingElement, RabbitMQTaskQueueBinding binding)
            : base(context, bingingElement, binding)
        {
        }

        protected override IRequestChannel OnCreateChannel(EndpointAddress address, Uri via)
        {
            return new RabbitMQTaskQueueRequestChannel(Context, this, address, via, BufferManager, Binding);
        }
    }
}
