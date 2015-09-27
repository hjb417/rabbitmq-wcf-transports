using System;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel
{
    internal delegate bool TryReceiveDelegate(TimeSpan timeout, out Message message);
}
