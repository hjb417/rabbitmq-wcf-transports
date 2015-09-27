using System;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel
{
    internal delegate bool TryReceiveRequestDelegate(TimeSpan timeout, out RequestContext context);
}
