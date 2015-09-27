using System;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal sealed class RabbitMQReaderWriterFactory : IRabbitMQReaderWriterFactory
    {
        static RabbitMQReaderWriterFactory()
        {
            Instance = new RabbitMQReaderWriterFactory();
        }

        private RabbitMQReaderWriterFactory()
        {
        }

        public static RabbitMQReaderWriterFactory Instance { get; private set; }

        public IRabbitMQReader CreateReader(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnClose, TimeSpan? queueTimeToLive, IDequeueThrottler throttler, TimeSpan timeout, CancellationToken cancelToken)
        {
            RabbitMQReader reader = null;
            try
            {
                reader = new RabbitMQReader(connectionFactory, exchange, queueName, isDurable, deleteQueueOnClose, queueTimeToLive, throttler);
                reader.EnsureOpen(timeout, cancelToken);
                return reader;
            }
            catch
            {
                DisposeHelper.DisposeIfNotNull(reader);
                throw;
            }
        }

        public IRabbitMQWriter CreateWriter(IConnectionFactory connectionFactory, TimeSpan timeout, CancellationToken cancelToken)
        {
            RabbitMQWriter writer = null;
            try
            {
                writer = new RabbitMQWriter(connectionFactory);
                writer.EnsureOpen(timeout, cancelToken);
                return writer;
            }
            catch
            {
                DisposeHelper.DisposeIfNotNull(writer);
                throw;
            }
        }
    }
}