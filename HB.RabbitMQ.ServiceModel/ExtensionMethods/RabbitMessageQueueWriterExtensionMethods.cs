using System;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace HB
{
    internal static class RabbitMessageQueueWriterExtensionMethods
    {
        public static void Enqueue(this IRabbitMQWriter rabbitMessageQueueWriter, string exchange, string queueName, Message message, BufferManager bufferManager, RabbitMQTaskQueueBinding binding, MessageEncoderFactory messageEncoderFactory, TimeSpan timeToLive, TimeSpan timeout, CancellationToken cancelToken)
        {
            var buffer = messageEncoderFactory.Encoder.WriteMessage(message, (int)binding.MaxReceivedMessageSize, bufferManager);
            try
            {
                using (var messageStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count))
                {
                    rabbitMessageQueueWriter.Enqueue(exchange, queueName, messageStream, timeToLive, timeout, cancelToken);
                }
            }
            finally
            {
                bufferManager.ReturnBuffer(buffer.Array);
            }
        }
    }
}