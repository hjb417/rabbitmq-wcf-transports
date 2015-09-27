using System;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal abstract class RabbitMQTaskQueueChannelBase : RabbitMQChannelBase
    {
        private readonly Action<TimeSpan> _onCloseFunc;
        private readonly Action<TimeSpan> _onOpenFunc;
        private readonly MessageEncoderFactory _msgEncoderFactory;

        protected RabbitMQTaskQueueChannelBase(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding)
            : base(channelManager, binding.CommunicationObjectCreatedCallback)
        {
            MethodInvocationTrace.Write();
            Binding = binding;
            BindingContext = context;
            _onCloseFunc = OnClose;
            _onOpenFunc = OnOpen;
            _msgEncoderFactory = context.GetMessageEncoderFactory();
        }

        protected RabbitMQTaskQueueBinding Binding { get; private set; }
        protected BindingContext BindingContext { get; private set; }
        protected ConcurrentOperationManager ConcurrentOperationManager { get; private set; }
        internal protected IRabbitMQWriter QueueWriter { get; protected set; }
        protected MessageEncoderFactory MessageEncoderFactory { get { return _msgEncoderFactory; } }
        
        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _onCloseFunc.BeginInvoke(timeout, callback, state);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _onOpenFunc.BeginInvoke(timeout, callback, state);
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _onCloseFunc.EndInvoke(result);
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            _onOpenFunc.EndInvoke(result);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            ConcurrentOperationManager = new ConcurrentOperationManager(GetType().FullName);
            QueueWriter = Binding.QueueReaderWriterFactory.CreateWriter(Binding.ConnectionFactory, timeout, ConcurrentOperationManager.Token);
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            base.OnClose(timeout, closeReason);
            DisposeHelper.DisposeIfNotNull(QueueWriter);
            DisposeHelper.DisposeIfNotNull(ConcurrentOperationManager);
        }
    }
}