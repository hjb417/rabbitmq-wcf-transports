using System;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal interface IRabbitMQReaderWriterFactory
    {
        IRabbitMQReader CreateReader(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnClose, TimeSpan? queueTimeToLive, IDequeueThrottler throttler, TimeSpan timeout, CancellationToken cancelToken);
        IRabbitMQWriter CreateWriter(IConnectionFactory connectionFactory, TimeSpan timeout, CancellationToken cancelToken);
    }
}