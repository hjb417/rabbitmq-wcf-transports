using System;
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace HB
{
    internal static class RabbitMessageQueueReaderExtensionMethods
    {
        public static Message Dequeue(this IRabbitMQReader rabbitMessageQueueReader, RabbitMQTaskQueueBinding binding, MessageEncoderFactory messageEncoderFactory, TimeSpan timeout, CancellationToken cancelToken)
        {
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            var msg = rabbitMessageQueueReader.Dequeue(timeoutTimer.RemainingTime, cancelToken);
            Message result;
            try
            {
                result = messageEncoderFactory.Encoder.ReadMessage(msg.Body, (int)binding.MaxReceivedMessageSize);
            }
            catch
            {
                rabbitMessageQueueReader.RejectMessage(msg.DeliveryTag, timeoutTimer.RemainingTime, cancelToken);
                throw;
            }
            rabbitMessageQueueReader.AcknowledgeMessage(msg.DeliveryTag, timeoutTimer.RemainingTime, cancelToken);
            return result;
        }
    }
}
