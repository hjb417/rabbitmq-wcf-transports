using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class TestConnectionFactory
    {
        public static readonly ConnectionFactory Instance = new ConnectionFactory { HostName = "localhost" };

        private TestConnectionFactory()
        {
        }
    }
}