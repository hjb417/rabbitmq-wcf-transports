using System;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal abstract class RabbitMQTaskQueueChannelListenerBase<TChannel> : RabbitMQChannelListenerBase<TChannel>, IChannelListener
        where TChannel : class, IChannel
    {
        private readonly Func<TimeSpan, TChannel> _onAcceptChannelFunc;
        private readonly Action<TimeSpan> _onCloseFunc;
        private readonly Uri _listenUri;
        private readonly Action<TimeSpan> _onOpenFunc;
        private readonly Func<TimeSpan, bool> _onWaitForChannelFunc;

        internal RabbitMQTaskQueueChannelListenerBase(BindingContext context, RabbitMQTaskQueueBinding binding)
            : base(binding)
        {
            MethodInvocationTrace.Write();
            Binding = binding;
            _onAcceptChannelFunc = OnAcceptChannel;
            _onCloseFunc = OnClose;
            _onOpenFunc = OnOpen;
            _onWaitForChannelFunc = OnWaitForChannel;
            Context = context;
            if (context.ListenUriMode == ListenUriMode.Unique)
            {
                var queueName = "l" + Guid.NewGuid().ToString("N");
                _listenUri = RabbitMQTaskQueueUri.Create(queueName, Constants.DefaultExchange, true, true, Binding.QueueTimeToLive);
            }
            else
            {
                _listenUri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
            }
        }

        public override Uri Uri { get { return _listenUri; } }

        protected ConcurrentOperationManager ConcurrentOperationManager { get; private set; }
        protected BufferManager BufferManager { get; private set; }
        protected RabbitMQTaskQueueBinding Binding { get; private set; }
        protected BindingContext Context { get; private set; }

        protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _onAcceptChannelFunc.BeginInvoke(timeout, callback, state);
        }

        protected override TChannel OnEndAcceptChannel(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _onAcceptChannelFunc.EndInvoke(result);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            MethodInvocationTrace.Write();
            return _onWaitForChannelFunc.BeginInvoke(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            MethodInvocationTrace.Write();
            return _onWaitForChannelFunc.EndInvoke(result);
        }

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
            ConcurrentOperationManager = new ServiceModel.ConcurrentOperationManager(GetType().FullName);
            BufferManager = Binding.CreateBufferManager();
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            DisposeHelper.DisposeIfNotNull(ConcurrentOperationManager);
            if (BufferManager != null)
            {
                BufferManager.Clear();
            }
            base.OnClose(timeout, closeReason);
        }
    }
}