using RabbitMQ.Client;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex
{
    public class RabbitMQLongProcessingServiceTests : LongProcessingServiceTests
    {
        private readonly string _queueName;

        public RabbitMQLongProcessingServiceTests(ITestOutputHelper outputHelper)
            : base(BindingTypes.RabbitMQTaskQueue, outputHelper)
        {
            _queueName = Server.QueueName;
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                var connFactory = new ConnectionFactory { HostName = "localhost" };
                using (var conn = connFactory.CreateConnection())
                using (var model = conn.CreateModel())
                {
                    model.QueueDeleteNoWait(_queueName, false, false);
                }
            }
            base.Dispose(disposing);
        }
    }
}