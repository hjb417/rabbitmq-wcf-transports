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