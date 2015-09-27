using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel
{
    internal abstract class RabbitMQChannelBase : ChannelBase, ICommunicationObject
    {
        protected RabbitMQChannelBase(ChannelManagerBase channelManager, Action<ICommunicationObject> communicationObjectCreatedCallback)
            : base(channelManager)
        {
            communicationObjectCreatedCallback.TryInvoke(this);
        }

        protected override sealed void OnAbort()
        {
            MethodInvocationTrace.Write();
            OnClose(TimeSpan.Zero, CloseReasons.Abort);
        }

        protected override sealed void OnClose(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            OnClose(timeout, CloseReasons.StateTransition);
        }

        protected virtual void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
        }
    }
}
