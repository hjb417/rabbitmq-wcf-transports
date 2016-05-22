using System.Messaging;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex
{
    public class MsMqLongProcessingServiceTests : LongProcessingServiceTests
    {
        private readonly string _queueName;

        public MsMqLongProcessingServiceTests(ITestOutputHelper outputHelper)
            : base(BindingTypes.DuplexMsmq, outputHelper)
        {
            _queueName = Server.QueueName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                MessageQueue.Delete(@".\private$\" + _queueName);
            }
            base.Dispose(disposing);
        }
    }
}