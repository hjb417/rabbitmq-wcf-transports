using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel
{
    internal abstract class RabbitMQChannelFactoryBase<TChannel> : ChannelFactoryBase<TChannel>
    {
        protected RabbitMQChannelFactoryBase(IDefaultCommunicationTimeouts timeouts)
            : base(timeouts)
        {
        }

        protected override sealed void OnAbort()
        {
            MethodInvocationTrace.Write();
            OnClose(TimeSpan.Zero, CloseReasons.Abort);
            base.OnAbort();
        }

        protected override sealed void OnClose(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            OnClose(timeoutTimer.RemainingTime, CloseReasons.StateTransition);
            base.OnClose(timeoutTimer.RemainingTime);
        }

        protected virtual void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
        }
    }
}
