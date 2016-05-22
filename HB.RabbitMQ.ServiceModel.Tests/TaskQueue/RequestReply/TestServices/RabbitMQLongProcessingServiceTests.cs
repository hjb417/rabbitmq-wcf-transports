using System;
using System.ServiceModel;
using RabbitMQ.Client;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices
{
    public class RabbitMQVanillaServiceTests : VanillaServiceTests
    {
        private readonly string _queueName;

        public RabbitMQVanillaServiceTests(ITestOutputHelper outputHelper)
            : base(BindingTypes.RabbitMQTaskQueue, outputHelper)
        {
            _queueName = Server.QueueName;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
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

        [Fact]
        public void ChannelStaysOpenOnServerErrorTest()
        {
            var errMsg = Guid.NewGuid().ToString();
            try
            {
                Client.Fail(errMsg);
                Assert.True(false);
            }
            catch(Exception e)
            {
                Assert.Contains(errMsg, e.ToString());
            }

            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Opened, s.State));
        }
    }
}