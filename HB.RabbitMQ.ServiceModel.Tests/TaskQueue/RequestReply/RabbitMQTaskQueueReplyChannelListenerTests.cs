using System;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;
using HB.RabbitMQ.ServiceModel.TaskQueue;
using HB.RabbitMQ.ServiceModel.TaskQueue.RequestReply;
using HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices.VanillaService;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply
{
    public class RabbitMQTaskQueueReplyChannelListenerTests : UnitTest
    {
        private readonly ServiceHost _svcHost;

        public RabbitMQTaskQueueReplyChannelListenerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            var binding = BindingFactory.Create(BindingTypes.RabbitMQTaskQueue, null, null);
            _svcHost = new ServiceHost(typeof(VanillaService));
            _svcHost.AddServiceEndpoint(typeof(IVanillaService), binding, RabbitMQTaskQueueUri.Create("localhost", 5672, Guid.NewGuid().ToString()));
            _svcHost.Open(TimeSpan.FromSeconds(30));
            var listener = _svcHost.ChannelDispatchers.Single().Listener;
            Listener = (RabbitMQTaskQueueReplyChannelListener)listener.GetType().GetProperty("InnerChannelListener", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(listener, null);
        }

        private RabbitMQTaskQueueReplyChannelListener Listener { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _svcHost.Abort();
            }
            base.Dispose(disposing);
        }

        [Fact]
        public void AcceptReturnsNullWhenListenerClosedWhileWaitingForIncomingRequestTest()
        {
            using (var acceptingEvent = new ManualResetEventSlim())
            {
                var getChannelTask = StartNewTask(() =>
                {
                    acceptingEvent.Set();
                    return Listener.AcceptChannel(TimeSpan.MaxValue);
                });
                acceptingEvent.Wait();
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Listener.Close(TimeSpan.MaxValue);
                WaitForTaskToFinish(getChannelTask, TimeSpan.FromSeconds(30));
                Assert.False(getChannelTask.IsFaulted);
                Assert.False(getChannelTask.IsCanceled);
                Assert.Null(getChannelTask.Result);
            }
        }
    }
}