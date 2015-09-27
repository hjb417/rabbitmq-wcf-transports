
using System;
namespace HB.RabbitMQ.ServiceModel.Tests
{
    [Serializable]
    public enum BindingTypes
    {
        NetTcp,
        Msmq,
        DuplexMsmq,
        RabbitMQTaskQueue,
    }
}