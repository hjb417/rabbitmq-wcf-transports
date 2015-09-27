using System;
using System.Runtime.Serialization;

namespace HB.RabbitMQ.ServiceModel
{
    [Serializable]
    public class RemoteQueueDoesNotExistException : Exception
    {
        public RemoteQueueDoesNotExistException(string exchange, string queueName)
            : this(string.Format("The remote queue [{0}] on the exchange [{1}] does not exist.", queueName, exchange))
        {
        }

        public RemoteQueueDoesNotExistException(string message)
            : base(message)
        {
        }

        protected RemoteQueueDoesNotExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}