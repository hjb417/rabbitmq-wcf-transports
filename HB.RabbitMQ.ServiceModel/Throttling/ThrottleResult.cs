using System;

namespace HB.RabbitMQ.ServiceModel.Throttling
{
    [Serializable]
    public enum ThrottleResult
    {
        TakeMessage,
        SkipMessage,
    }
}