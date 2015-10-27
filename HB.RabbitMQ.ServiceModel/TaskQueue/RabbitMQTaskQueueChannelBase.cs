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
            QueueWriter = Binding.QueueReaderWriterFactory.CreateWriter(Binding.ConnectionFactory, timeout, ConcurrentOperationManager.Token, Binding.WriterOptions);
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
