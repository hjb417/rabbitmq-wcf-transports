using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public static partial class ClientBaseFactory
    {
        public static ClientBase<TService> Create<TService>(Binding binding, string remoteAddress)
            where TService : class
        {
            return new ClientBase<TService>(binding, remoteAddress);
        }
    }
}