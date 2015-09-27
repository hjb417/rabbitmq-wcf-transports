using System;
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class RabbitMQConnectionBaseTests
    {
        internal class RabbitMQQueueConnection : RabbitMQConnectionBase
        {
            public RabbitMQQueueConnection(IConnectionFactory connectionFactory)
                : base(connectionFactory)
            {
            }

            public RabbitMQQueueConnection(IConnectionFactory connectionFactory, string queueName, bool closeOnDispose)
                : base(connectionFactory, queueName, closeOnDispose)
            {
            }

            protected override void InitializeModel(IModel model)
            {
            }

            new public void PerformAction(Action<IModel> action, TimeSpan timeout, CancellationToken cancelToken)
            {
                base.PerformAction(action, timeout, cancelToken);
            }

            new public T PerformAction<T>(Func<IModel, T> action, TimeSpan timeout, CancellationToken cancelToken)
            {
                return base.PerformAction<T>(action, timeout, cancelToken);
            }
        }
    }
}