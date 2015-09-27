using System;
using System.IO;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel
{
    internal interface IRabbitMQWriter : IDisposable
    {
        void Enqueue(string exchange, string queueName, Stream messageStream, TimeSpan timeToLive, TimeSpan timeout, CancellationToken cancelToken);
    }
}