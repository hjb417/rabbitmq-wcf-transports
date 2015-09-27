using System;

namespace HB.RabbitMQ.ServiceModel
{
    [Serializable]
    internal enum CloseReasons
    {
        Abort,
        StateTransition,
    }
}