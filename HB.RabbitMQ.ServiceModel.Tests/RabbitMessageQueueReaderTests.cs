using System;
using System.Diagnostics;
using System.Threading;
using HB.RabbitMQ.ServiceModel.TaskQueue;
using HB.RabbitMQ.ServiceModel.Throttling;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class RabbitMessageQueueReaderTests : UnitTest
    {
        public RabbitMessageQueueReaderTests()
        {
            Reader = new RabbitMQReader(TestConnectionFactory.Instance, "amq.direct", Guid.NewGuid().ToString(), false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions());
        }

        private RabbitMQReader Reader { get; set; }

        [Fact]
        public void WaitForMessageReturnsFalseWhenNoMessageIsAvailableTest()
        {
            var hasMsg = Reader.WaitForMessage(TimeSpan.FromTicks(1), CancellationToken.None);
            Assert.False(hasMsg);
        }

        [Fact]
        public void WaitForMessageReturnsTrueWhenMessageIsAvailableTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            uint msgCount = 0;

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);
            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 0, 0));
            model.QueueDeclarePassive(null).ReturnsForAnyArgs(x => new QueueDeclareOk(string.Empty, Thread.VolatileRead(ref msgCount), 0));

            var delay = TimeSpan.FromSeconds(15);
            using (var rdr = new RabbitMQReader(connFactory, null, null, false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions()))
            using (var timer = new Timer(state => Thread.VolatileWrite(ref msgCount, 1), null, delay, TimeSpan.Zero))
            {
                var stopwatch = Stopwatch.StartNew();
                var gotMsg = rdr.WaitForMessage(TimeSpan.FromSeconds(90), CancellationToken.None);
                stopwatch.Stop();
                Assert.True(gotMsg);
                Assert.True(stopwatch.Elapsed >= delay);
            }
        }

        [Fact]
        public void WaitForMessageWaitsForNoLessThanSpecifiedTimeSpanTest()
        {
            var timeout = TimeSpan.FromSeconds(15);
            var timer = Stopwatch.StartNew();
            Reader.WaitForMessage(timeout, CancellationToken.None);
            timer.Stop();
            Assert.True(timer.Elapsed >= timeout);
        }

        [Fact]
        public void DequeueThrowsWhenNoMessageReceivedWithintimeoutTest()
        {
            Exception error = null;
            try
            {
                Reader.Dequeue(TimeSpan.FromTicks(1), CancellationToken.None);
            }
            catch (TimeoutException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void DequeueWaitsForNoLessThanSpecifiedTimeSpanTest()
        {
            Exception error = null;
            var timeout = TimeSpan.FromSeconds(15);
            var timer = Stopwatch.StartNew();
            try
            {
                Reader.Dequeue(timeout, CancellationToken.None);
            }
            catch (TimeoutException e)
            {
                error = e;
            }
            timer.Stop();
            Assert.True(timer.Elapsed >= timeout);
            Assert.NotNull(error);
        }

        [Fact]
        public void DequeueThrowsWhenCancellationTokenIsCancelledTest()
        {
            Exception error = null;
            using (CancellationTokenSource src = new CancellationTokenSource())
            {
                var timeout = TimeSpan.FromSeconds(30);
                src.CancelAfter(timeout);
                var timer = Stopwatch.StartNew();
                try
                {
                    Reader.Dequeue(TimeSpan.FromSeconds(90), src.Token);
                }
                catch (OperationCanceledException e)
                {
                    error = e;
                }
                timer.Stop();
                Assert.True(timer.Elapsed >= timeout);
                Assert.NotNull(error);
            }
        }

        [Fact]
        public void DisposeDisposesConnectionTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);

            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 0, 0));

            using (var rdr = new RabbitMQReader(connFactory, null, null, false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions()))
            {
                rdr.EnsureOpen(TimeSpan.FromSeconds(30), CancellationToken.None);
                conn.DidNotReceive().Dispose();
            }
            conn.Received().Dispose();
        }

        [Fact]
        public void DisposeDisposesModelTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);
            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 0, 0));

            using (var rdr = new RabbitMQReader(connFactory, null, null, false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions()))
            {
                rdr.EnsureOpen(TimeSpan.FromSeconds(30), CancellationToken.None);
                model.DidNotReceive().Dispose();
            }
            model.Received().Dispose();
        }

        [Fact]
        public void DequeueThrowsWhenDisposedTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            var throttler = Substitute.For<IDequeueThrottler>();

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);
            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 0, 0));
            model.QueueDeclarePassive(null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 1, 0));

            using (var rdr = new RabbitMQReader(connFactory, null, null, false, true, TimeSpan.FromMinutes(20), throttler, new RabbitMQReaderOptions()))
            {
                throttler.WhenForAnyArgs(x => x.Throttle(0, 0, CancellationToken.None)).Do(x =>
                {
                    ThreadPool.QueueUserWorkItem(state =>
                    {
                        try
                        {
                            rdr.Dispose();
                        }
                        catch { }
                    });
                });
                Exception error = null;
                try
                {
                    rdr.Dequeue(TimeSpan.FromSeconds(90), CancellationToken.None);
                }
                catch (ObjectDisposedException e)
                {
                    error = e;
                }
                Assert.NotNull(error);
            }
        }

        [Fact]
        public void DequeueReturnsMessageTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();

            var queueName = Guid.NewGuid().ToString();

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);
            conn.IsOpen.Returns(true);
            model.IsClosed.Returns(false);
            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(queueName, 1, 0));
            model.QueueDeclarePassive(null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 1, 0));
            var payload = Guid.NewGuid().ToByteArray();
            var result = new BasicGetResult(0, false, null, null, 0, null, payload);
            model.BasicGet(queueName, false).Returns(result);

            using (var rdr = new RabbitMQReader(connFactory, Constants.DefaultExchange, queueName, false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions()))
            {
                var msg = rdr.Dequeue(TimeSpan.FromSeconds(30), CancellationToken.None);
                Assert.Equal(payload, msg.Body.CopyToByteArray());
            }
        }

        [Fact]
        public void AcknowledgeMessageCallsBasicAckTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();

            connFactory.CreateConnection().Returns(conn);
            conn.CreateModel().Returns(model);
            model.QueueDeclare(null, false, false, false, null).ReturnsForAnyArgs(new QueueDeclareOk(string.Empty, 0, 0));

            using (var rdr = new RabbitMQReader(connFactory, null, null, false, true, TimeSpan.FromMinutes(20), NoOpDequeueThrottler.Instance, new RabbitMQReaderOptions()))
            {
                rdr.AcknowledgeMessage(5, TimeSpan.FromSeconds(90), CancellationToken.None);
                model.Received().BasicAck(5, false);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Reader.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}