using System;
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal sealed class RabbitMQWriterConnection : RabbitMQConnectionBase
    {
        public RabbitMQWriterConnection(IConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
            MethodInvocationTrace.Write();
        }

        protected override void InitializeModel(IModel model)
        {
            model.ConfirmSelect();
        }

        public IBasicProperties CreateBasicProperties(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            return PerformAction(model => model.CreateBasicProperties(), timeout, cancelToken);
        }
    }
}