using System;
using System.Runtime.Serialization;

namespace HB.RabbitMQ.ServiceModel
{
    [Serializable]
    public class MessageNotAcknowledgedByBrokerException : Exception
    {
        public MessageNotAcknowledgedByBrokerException(string exchange, string queueName)
            : this(string.Format("The broker was unable to process the message to the remote queue [{0}] on the exchange [{1}].", queueName, exchange))
        {
        }

        public MessageNotAcknowledgedByBrokerException(string message)
            : base(message)
        {
        }

        protected MessageNotAcknowledgedByBrokerException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}