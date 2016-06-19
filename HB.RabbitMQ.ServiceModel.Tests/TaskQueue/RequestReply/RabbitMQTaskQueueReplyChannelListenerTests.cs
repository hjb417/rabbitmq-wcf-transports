using System;
using System.Diagnostics;
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
            ServerUri = RabbitMQTaskQueueUri.Create("localhost", 5672, Guid.NewGuid().ToString());
            _svcHost = new ServiceHost(typeof(VanillaService));
            _svcHost.AddServiceEndpoint(typeof(IVanillaService), binding, ServerUri);
            _svcHost.Open(TimeSpan.FromSeconds(30));
            var listener = _svcHost.ChannelDispatchers.Single().Listener;
            Listener = (RabbitMQTaskQueueReplyChannelListener)listener.GetType().GetProperty("InnerChannelListener", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(listener, null);
        }

        private RabbitMQTaskQueueReplyChannelListener Listener { get; set; }
        private Uri ServerUri { get; }

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

        [Fact]
        public void AcceptChannelThrowsTimeoutExceptionWhenTimeoutReachedTest()
        {
            var acceptChannelTask = StartNewTask(() => Listener.AcceptChannel(TimeSpan.FromSeconds(10)));
            try
            {
                acceptChannelTask.Wait(TimeSpan.FromSeconds(30));
            }
            catch { }
            Assert.True(acceptChannelTask.IsFaulted);
            Assert.IsType(typeof(TimeoutException), acceptChannelTask.Exception.InnerExceptions.Single());
        }

        [Fact]
        public void AcceptChannelReturnsNullWhenTheListenerIsClosedTest()
        {
            Listener.Close();
            var acceptChannelTask = StartNewTask(() => Listener.AcceptChannel(TimeSpan.FromSeconds(10)));
            Assert.True(acceptChannelTask.Wait(TimeSpan.FromSeconds(30)));
            Assert.False(acceptChannelTask.IsFaulted);
            Assert.Null(acceptChannelTask.Result);
        }

        [Fact]
        public void WaitForChannelReturnsFalseWhenThereAreNoChannelsTest()
        {
            Stopwatch timer = null;
            var waitForChannel = StartNewTask(() =>
            {
                timer = Stopwatch.StartNew();
                var hasChannel = Listener.WaitForChannel(TimeSpan.FromSeconds(1));
                timer.Stop();
                return hasChannel;
            });
            Assert.True(waitForChannel.Wait(TimeSpan.FromSeconds(30)));
            Assert.False(waitForChannel.Result);
            Assert.True(timer.Elapsed < TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void WaitForChannelWaitsForRequestedTimeForChannelTest()
        {
            var waitTime = TimeSpan.FromSeconds(15);
            Stopwatch timer = null;
            var waitForChannel = StartNewTask(() =>
            {
                timer = Stopwatch.StartNew();
                var hasChannel = Listener.WaitForChannel(waitTime);
                timer.Stop();
                return hasChannel;
            });
            Assert.True(waitForChannel.Wait(TimeSpan.FromSeconds(30)));
            Assert.False(waitForChannel.Result);
            Assert.True(timer.Elapsed >= waitTime);
            Assert.True(timer.Elapsed <= waitTime.Add(TimeSpan.FromSeconds(2)));
        }
    }
}