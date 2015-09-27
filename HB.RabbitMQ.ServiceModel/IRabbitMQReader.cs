using System;
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal interface IRabbitMQReader : IDisposable
    {
        string Exchange { get; }
        string QueueName { get; }
        void AcknowledgeMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken);
        void RejectMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken);
        QueueDeclareOk QueryQueue(TimeSpan timeout, CancellationToken cancelToken);
        DequeueResult Dequeue(TimeSpan timeout, CancellationToken cancelToken);
        bool WaitForMessage(TimeSpan timeout, CancellationToken cancelToken);
        void SoftClose();
    }
}