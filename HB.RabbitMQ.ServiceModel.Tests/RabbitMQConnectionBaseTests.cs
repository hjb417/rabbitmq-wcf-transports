using System;
using System.IO;
using System.Threading;
using HB.RabbitMQ.ServiceModel.TaskQueue;
using NSubstitute;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public partial class RabbitMQConnectionBaseTests : UnitTest
    {
        public RabbitMQConnectionBaseTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            Connection = new RabbitMQQueueConnection(connFactory);
        }

        private RabbitMQQueueConnection Connection { get; set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Connection.Dispose();
            }
            base.Dispose(disposing);
        }

        [Fact]
        public void PerformActionThrowsWhenTimeoutReachedTest()
        {
            Exception error = null;
            try
            {
                Connection.PerformAction(delegate { }, TimeSpan.Zero, CancellationToken.None);
            }
            catch (TimeoutException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void GenericPerformActionThrowsWhenTimeoutReachedTest()
        {
            Exception error = null;
            try
            {
                Connection.PerformAction<object>(delegate { return null; }, TimeSpan.Zero, CancellationToken.None);
            }
            catch (TimeoutException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void PerformActionThrowsWhenDisposedTest()
        {
            Exception error = null;
            Connection.Dispose();
            try
            {
                Connection.PerformAction(delegate { }, TimeSpan.FromSeconds(30), CancellationToken.None);
            }
            catch (ObjectDisposedException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void PerformActionReturnsValueReturnedByFuncTest()
        {
            var expected = Guid.NewGuid();
            var actual = this.Connection.PerformAction(m => expected, TimeSpan.FromSeconds(30), CancellationToken.None);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GenericPerformActionThrowsWhenDisposedTest()
        {
            Exception error = null;
            Connection.Dispose();
            try
            {
                Connection.PerformAction<object>(delegate { return null; }, TimeSpan.FromSeconds(30), CancellationToken.None);
            }
            catch (ObjectDisposedException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void ReaderIsClosedOnDisposedWhenSetToTrueTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, true);
            conn.EnsureConnectionOpen(TimeSpan.FromSeconds(30), CancellationToken.None);
            conn.Dispose();
            model.Received().QueueDeleteNoWait(queueName, false, false);
        }

        [Fact]
        public void ReaderIsNotClosedOnDisposedWhenSetToFalseTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, false);
            conn.EnsureConnectionOpen(TimeSpan.FromSeconds(30), CancellationToken.None);
            conn.Dispose();
            model.DidNotReceiveWithAnyArgs().QueueDeleteNoWait(null, false, false);
        }

        [Fact]
        public void BasicPublishThrowsWhenBasicAcksNotReceivedTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, false);
            Exception error = null;
            try
            {
                conn.BasicPublish(Constants.DefaultExchange, queueName, null, new MemoryStream(), TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            catch(TimeoutException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void BasicPublishReturnsWhenBasicAcksReceivedTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(c => model.BasicAcks += Raise.EventWith<BasicAckEventArgs>());
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, false);
            conn.BasicPublish(Constants.DefaultExchange, queueName, null, new MemoryStream(), TimeSpan.FromSeconds(5), CancellationToken.None);
        }

        [Fact]
        public void BasicPublishThrowsWhenBasicNacksEventRaisedTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(c => model.BasicNacks += Raise.EventWith<BasicNackEventArgs>());
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, false);
            Exception error = null;
            try
            {
                conn.BasicPublish(Constants.DefaultExchange, queueName, null, new MemoryStream(), TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            catch (MessageNotAcknowledgedByBrokerException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void BasicPublishThrowsWhenBasicReturnEventRaisedTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            model.WhenForAnyArgs(x => x.BasicPublish(null, null, false, null, null)).Do(c => model.BasicReturn += Raise.EventWith<BasicReturnEventArgs>());
            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, false);
            Exception error = null;
            try
            {
                conn.BasicPublish(Constants.DefaultExchange, queueName, null, new MemoryStream(), TimeSpan.FromSeconds(5), CancellationToken.None);
            }
            catch (RemoteQueueDoesNotExistException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void ExceptionsAreSuppressedWhenClosingQueueTest()
        {
            var connFactory = Substitute.For<IConnectionFactory>();
            var mockConn = Substitute.For<IConnection>();
            var model = Substitute.For<IModel>();
            model.WhenForAnyArgs(x => x.QueueDeleteNoWait(null, false, false)).Do(x => { throw new Exception(); });

            connFactory.CreateConnection().Returns(mockConn);
            connFactory.CreateConnection(string.Empty).ReturnsForAnyArgs(mockConn);
            mockConn.CreateModel().Returns(model);

            var queueName = Guid.NewGuid().ToString();

            var conn = new RabbitMQQueueConnection(connFactory, queueName, true);
            conn.EnsureConnectionOpen(TimeSpan.FromSeconds(30), CancellationToken.None);
            conn.Dispose();
        }
    }
}