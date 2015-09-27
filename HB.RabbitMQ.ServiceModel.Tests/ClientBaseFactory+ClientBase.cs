using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class ClientBaseFactory
    {
        public sealed class ClientBase<TService> : System.ServiceModel.ClientBase<TService>
            where TService : class
        {
            public ClientBase(Binding binding, string remoteAddress)
                : base(binding, new EndpointAddress(remoteAddress))
            {
            }

            public new TService Channel { get { return base.Channel; } }
        }
    }
}