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
using HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    public class RabbitMQTaskQueueListenerAdapter : IDisposable
    {
        private RabbitMQTaskQueueListenerAdapterInstance _adapter;
        private readonly Func<IListenerAdapter> _listenerAdapterFactory;
        private readonly Func<IRabbitMQQueueMonitor> _queueMionitorFactory;
        private readonly object _recreateLock = new object();
        private volatile bool _isDisposed;

        public RabbitMQTaskQueueListenerAdapter(Uri rabbitMqManagementUri, TimeSpan pollInterval)
            : this(() => new ListenerAdapter(Constants.Scheme), () => new RabbitMQQueueMonitor(rabbitMqManagementUri, pollInterval))
        {
        }

        internal RabbitMQTaskQueueListenerAdapter(Func<IListenerAdapter> listenerAdapterFactory, Func<IRabbitMQQueueMonitor> queueMionitorFactory)
        {
            _listenerAdapterFactory = listenerAdapterFactory;
            _queueMionitorFactory = queueMionitorFactory;
            RecreateListenerAdapter();
        }

        ~RabbitMQTaskQueueListenerAdapter()
        {
            Dispose(false);
        }

        private void OnMaxListenerChannelIdReached(object sender, EventArgs e)
        {
            TraceInformation($"Recreating instance of [{nameof(RabbitMQTaskQueueListenerAdapterInstance)}] because the max channel id has been reached.", GetType());
            RecreateListenerAdapter();
        }

        private void RecreateListenerAdapter()
        {
            lock (_recreateLock)
            {
                DisposeHelper.DisposeIfNotNull(_adapter);
                if (!_isDisposed)
                {
                    TraceInformation($"Starting an new instance of [{nameof(RabbitMQTaskQueueListenerAdapterInstance)}].", GetType());
                    _adapter = new RabbitMQTaskQueueListenerAdapterInstance(_listenerAdapterFactory, _queueMionitorFactory);
                    _adapter.MaxListenerChannelIdReached += OnMaxListenerChannelIdReached;
                    _adapter.Initialize();
                    TraceInformation($"Started an new instance of [{nameof(RabbitMQTaskQueueListenerAdapterInstance)}].", GetType());
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                lock(_recreateLock)
                {
                    DisposeHelper.DisposeIfNotNull(_adapter);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}