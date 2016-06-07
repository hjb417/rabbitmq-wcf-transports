/*
Copyright (c) 2015 HJB417

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal abstract class RabbitMQTaskQueueChannelBase : RabbitMQChannelBase
    {
        private readonly Action<TimeSpan> _onCloseFunc;
        private readonly Action<TimeSpan> _onOpenFunc;
        private readonly MessageEncoderFactory _msgEncoderFactory;

        protected RabbitMQTaskQueueChannelBase(BindingContext context, ChannelManagerBase channelManager, RabbitMQTaskQueueBinding binding, EndpointAddress localAddress)
            : base(channelManager, binding.CommunicationObjectCreatedCallback)
        {
            MethodInvocationTrace.Write();
            LocalAddress = localAddress;
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
        public EndpointAddress LocalAddress { get; }

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
            var connFactory = Binding.CreateConnectionFactory(LocalAddress.Uri.Host, LocalAddress.Uri.Port);
            var writerSetup = new RabbitMQWriterSetup
            {
                CancelToken = ConcurrentOperationManager.Token,
                ConnectionFactory = connFactory,
                Options = Binding.WriterOptions,
                Timeout = timeout,
            };
            QueueWriter = Binding.QueueReaderWriterFactory.CreateWriter(writerSetup);
        }

        protected override void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
            MethodInvocationTrace.Write();
            try
            {
                base.OnClose(timeout, closeReason);
            }
            finally
            {
                DisposeHelper.DisposeIfNotNull(QueueWriter);
                DisposeHelper.DisposeIfNotNull(ConcurrentOperationManager);
            }
        }
    }
}
