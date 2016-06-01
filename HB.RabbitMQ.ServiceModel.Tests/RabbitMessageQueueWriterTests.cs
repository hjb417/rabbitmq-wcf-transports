using System;
using System.IO;
using System.Threading;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class RabbitMessageQueueWriterTests : UnitTest
    {
        public RabbitMessageQueueWriterTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public void ConfirmSelectIsEnabledTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();

            connFactory.CreateConnection().Returns(conn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(conn);
            conn.CreateModel().Returns(model);

            var setup = new RabbitMQWriterSetup
            {
                ConnectionFactory = connFactory,
                Options = new RabbitMQWriterOptions(),
            };
            using (var rdr = new RabbitMQWriter(setup, false))
            {
                rdr.EnsureOpen(TimeSpan.FromSeconds(90), CancellationToken.None);
                model.Received().ConfirmSelect();
            }
        }

        [Fact]
        public void EnqueuedMessagesArePersistentTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            var msg = new MemoryStream(Guid.NewGuid().ToByteArray());

            connFactory.CreateConnection().Returns(conn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(conn);
            conn.CreateModel().Returns(model);

            var setup = new RabbitMQWriterSetup
            {
                ConnectionFactory = connFactory,
                Options = new RabbitMQWriterOptions(),
            };
            using (var writer = new RabbitMQWriter(setup, false))
            {
                IBasicProperties props = null;
                model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(ci =>
                {
                    props = ci.Arg<IBasicProperties>();
                    model.BasicAcks += Raise.EventWith(model, new BasicAckEventArgs());
                });
                writer.Enqueue(null, null, msg, TimeSpan.MaxValue, TimeSpan.FromSeconds(90), CancellationToken.None);
                Assert.NotNull(props);
                Assert.True(props.Persistent);
            }
        }

        [Fact]
        public void TimeToLiveIsAppliedToMessageTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            var msg = new MemoryStream(Guid.NewGuid().ToByteArray());
            var ttl = TimeSpan.FromSeconds(5);

            connFactory.CreateConnection().Returns(conn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(conn);
            conn.CreateModel().Returns(model);

            var setup = new RabbitMQWriterSetup
            {
                ConnectionFactory = connFactory,
                Options = new RabbitMQWriterOptions(),
            };
            using (var writer = new RabbitMQWriter(setup, false))
            {
                IBasicProperties props = null;
                model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(ci =>
                {
                    props = ci.Arg<IBasicProperties>();
                    model.BasicAcks += Raise.EventWith(model, new BasicAckEventArgs());
                });
                writer.Enqueue(null, null, msg, ttl, TimeSpan.FromSeconds(90), CancellationToken.None);
                Assert.NotNull(props);
                Assert.Equal(ttl.ToMillisecondsTimeout().ToString(), props.Expiration);
            }
        }

        [Fact]
        public void TimeToLiveIsNotAppliedToMessageForTimeSpanMaxValueTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            var msg = new MemoryStream(Guid.NewGuid().ToByteArray());

            connFactory.CreateConnection().Returns(conn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(conn);
            conn.CreateModel().Returns(model);

            var setup = new RabbitMQWriterSetup
            {
                ConnectionFactory = connFactory,
                Options = new RabbitMQWriterOptions(),
            };
            using (var writer = new RabbitMQWriter(setup, false))
            {
                IBasicProperties props = null;
                model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(ci =>
                {
                    props = ci.Arg<IBasicProperties>();
                    model.BasicAcks += Raise.EventWith(model, new BasicAckEventArgs());
                });
                writer.Enqueue(null, null, msg, TimeSpan.MaxValue, TimeSpan.FromSeconds(90), CancellationToken.None);
                Assert.NotNull(props);
                Assert.Empty(props.Expiration);
            }
        }

        [Fact]
        public void EnqueueUsesPassedInExchangeAndQueueNameTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var conn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(ci => model.BasicAcks += Raise.EventWith(model, new BasicAckEventArgs()));

            connFactory.CreateConnection().Returns(conn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(conn);
            conn.CreateModel().Returns(model);

            var setup = new RabbitMQWriterSetup
            {
                ConnectionFactory = connFactory,
                Options = new RabbitMQWriterOptions(),
            };
            using (var writer = new RabbitMQWriter(setup, false))
            {
                var exchange = Guid.NewGuid().ToString();
                var queueName = Guid.NewGuid().ToString();
                var msg = Guid.NewGuid();
                writer.Enqueue(exchange, queueName, new MemoryStream(msg.ToByteArray()), TimeSpan.MaxValue, TimeSpan.FromSeconds(30), CancellationToken.None);
                model.Received(1).BasicPublish(exchange, queueName, true, Arg.Any<IBasicProperties>(), Arg.Is<byte[]>(m => new Guid(m) == msg));
            }
        }
    }
}