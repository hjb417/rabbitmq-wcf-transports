using System;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal abstract class RabbitMQTaskQueueChannelFactoryBase<TChannel> : RabbitMQChannelFactoryBase<TChannel>, IChannelFactory
    {
        private readonly Action<TimeSpan> _openFunc;

        public RabbitMQTaskQueueChannelFactoryBase(BindingContext context, RabbitMQTransportBindingElement bindingElement, RabbitMQTaskQueueBinding binding)
            : base(binding)
        {
            MethodInvocationTrace.Write();
            Context = context;
            _openFunc = Open;
            BindingElement = bindingElement;
            Binding = binding;
            Binding.CommunicationObjectCreatedCallback.TryInvoke(this);
        }

        internal protected BufferManager BufferManager { get; private set; }
        protected BindingContext Context { get; private set; }
        protected RabbitMQTaskQueueBinding Binding { get; private set; }
        protected RabbitMQTransportBindingElement BindingElement { get; private set; }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _openFunc.BeginInvoke(timeout, callback, state);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _openFunc.EndInvoke(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            BufferManager = Binding.CreateBufferManager();
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            base.OnClose(timeout, closeReason);
            if (BufferManager != null)
            {
                BufferManager.Clear();
            }
        }
    }
}