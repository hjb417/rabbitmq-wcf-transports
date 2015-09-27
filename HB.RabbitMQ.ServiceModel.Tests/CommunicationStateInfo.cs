using System;
using System.Linq;
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    [Serializable]
    public class CommunicationStateInfo
    {
        public CommunicationStateInfo(Type type, CommunicationState state)
        {
            Type = type;
            State = state;
        }

        public CommunicationState State { get; private set; }
        public Type Type { get; private set; }

        public bool ContainsState(params CommunicationState[] states)
        {
            return states.Contains(State);
        }
    }
}