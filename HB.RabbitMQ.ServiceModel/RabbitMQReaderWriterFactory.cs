/*
Copyright (c) 2015 HJB417

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
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

        public IRabbitMQReader CreateReader(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnClose, TimeSpan? queueTimeToLive, TimeSpan timeout, CancellationToken cancelToken, RabbitMQReaderOptions options)
        {
            RabbitMQReader reader = null;
            try
            {
                reader = new RabbitMQReader(connectionFactory, exchange, queueName, isDurable, deleteQueueOnClose, queueTimeToLive, options);
                reader.EnsureOpen(timeout, cancelToken);
                return reader;
            }
            catch
            {
                DisposeHelper.DisposeIfNotNull(reader);
                throw;
            }
        }

        public IRabbitMQWriter CreateWriter(IConnectionFactory connectionFactory, TimeSpan timeout, CancellationToken cancelToken, RabbitMQWriterOptions options)
        {
            RabbitMQWriter writer = null;
            try
            {
                writer = new RabbitMQWriter(connectionFactory, options);
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