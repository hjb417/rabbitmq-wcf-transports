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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace HB.RabbitMQ.ServiceModel
{
    internal abstract class RabbitMQConnectionBase : IDisposable
    {
        private Tuple<IModel, IConnection> _modelAndConnection;
        private readonly IConnectionFactory _connFactory;
        private readonly object _modelLock = new object();
        private volatile bool _disposeRequested;
        private readonly string _queueName;
        private bool _deleteQueueOnDispose;

        protected RabbitMQConnectionBase(IConnectionFactory connectionFactory)
        {
            MethodInvocationTrace.Write();
            _connFactory = connectionFactory;
        }

        protected RabbitMQConnectionBase(IConnectionFactory connectionFactory, string queueName, bool deleteQueueOnDispose)
            : this(connectionFactory)
        {
            _deleteQueueOnDispose = deleteQueueOnDispose;
            _queueName = queueName;
            MethodInvocationTrace.Write();
        }

        [ExcludeFromCodeCoverage]
        ~RabbitMQConnectionBase()
        {
            Dispose(false);
        }

        private void DisposeModelAndConnection(ref Tuple<IModel, IConnection> modelAndConnection)
        {
            if (modelAndConnection == null)
            {
                return;
            }
            DisposeHelper.SilentDispose(modelAndConnection.Item1);
            DisposeHelper.SilentDispose(modelAndConnection.Item2);
            modelAndConnection = null;
        }

        private void ThrowIfDisposeRequested()
        {
            if (_disposeRequested)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }
        }

        public void EnsureConnectionOpen(TimeSpan timeout, CancellationToken cancelToken)
        {
            PerformAction(m => { }, timeout, cancelToken);
        }

        private void TryEnterLock(object obj, TimeoutTimer timeoutTimer, ref bool lockTaken, CancellationToken cancelToken)
        {
            lockTaken = false;
            while (!lockTaken)
            {
                ThrowIfDisposeRequested();
                timeoutTimer.ThrowIfNoTimeRemaining();
                cancelToken.ThrowIfCancellationRequested();
                Monitor.TryEnter(obj, 1, ref lockTaken);
            }
        }

        protected T PerformAction<T>(Func<IModel, T> action, TimeSpan timeout, CancellationToken cancelToken)
        {
            var result = default(T);
            PerformAction(new Action<IModel>(model => result = action(model)), timeout, cancelToken);
            return result;
        }

        protected void PerformAction(Action<IModel> action, TimeSpan timeout, CancellationToken cancelToken)
        {
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            bool lockTaken = false;
            bool reconnect = false;
            try
            {
                TryEnterLock(_modelLock, timeoutTimer, ref lockTaken, cancelToken);
                while (true)
                {
                    if (_modelAndConnection == null)
                    {
                        reconnect = true;
                    }
                    else if (_modelAndConnection.Item1.IsClosed || !_modelAndConnection.Item2.IsOpen)
                    {
                        reconnect = true;
                    }
                    if (reconnect)
                    {
                        DisposeModelAndConnection(ref _modelAndConnection);
                        _modelAndConnection = Connect(timeoutTimer, cancelToken);
                        reconnect = false;
                    }
                    try
                    {
                        action(_modelAndConnection.Item1);
                        return;
                    }
                    catch (OperationInterruptedException e)
                    {
                        Trace.TraceWarning("[{1}] Retrying connection to the server due to the error --> {0}", e, GetType());
                        reconnect = true;
                    }
                    catch (IOException e)
                    {
                        Trace.TraceWarning("[{1}] Retrying connection to the server due to the error --> {0}", e, GetType());
                        reconnect = true;
                    }
                    //wait 5 seconds before retrying
                    var waitTime = TimeSpanHelper.Min(timeoutTimer.RemainingTime, TimeSpan.FromSeconds(5));
                    cancelToken.WaitHandle.WaitOne(waitTime);
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_modelLock);
                }
            }
        }

        protected abstract void InitializeModel(IModel model);

        private Tuple<IModel, IConnection> Connect(TimeoutTimer timeoutTimer, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            IConnection conn = null;
            IModel model = null;
            while (true)
            {
                timeoutTimer.ThrowIfNoTimeRemaining();
                ThrowIfDisposeRequested();
                cancelToken.ThrowIfCancellationRequested();
                try
                {
                    conn = _connFactory.CreateConnection();
                    model = conn.CreateModel();
                    InitializeModel(model);
                    return Tuple.Create(model, conn);
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("[{1}] Retrying connection to the server due to the error --> {0}", e, GetType());
                    DisposeHelper.SilentDispose(model);
                    DisposeHelper.SilentDispose(conn);
                }
            }
        }

        public void BasicAck(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            PerformAction(model => model.BasicAck(deliveryTag, false), timeout, cancelToken);
        }

        public void BasicPublish(string exchange, string queueName, IBasicProperties messageProperties, Stream messageStream, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            var timer = TimeoutTimer.StartNew(timeout);
            var message = messageStream.CopyToByteArray();
            PerformAction(model =>
            {
                var confirmEvent = new ManualResetEventSlim();
                Exception error = null;
                int disposeEvent = 0;

                Action setConfirmEvent = () =>
                {
                    confirmEvent.Set();
                    if (Interlocked.CompareExchange(ref disposeEvent, 1, 1) == 1)
                    {
                        confirmEvent.Dispose();
                    }
                };

                EventHandler<BasicAckEventArgs> ackCallback = delegate { setConfirmEvent(); };
                EventHandler<BasicReturnEventArgs> returnCallback = delegate
                {
                    error = new RemoteQueueDoesNotExistException(exchange, queueName);
                    setConfirmEvent();
                };
                EventHandler<BasicNackEventArgs> nackCallback = delegate
                {
                    error = new MessageNotAcknowledgedByBrokerException(exchange, queueName);
                    setConfirmEvent();
                };
                try
                {
                    model.BasicAcks += ackCallback;
                    model.BasicReturn += returnCallback;
                    model.BasicNacks += nackCallback;

                    Debug.WriteLine("{0}-{1}: Publishing message to queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, queueName);
                    model.BasicPublish(exchange, queueName, true, messageProperties, message);
                    if (confirmEvent.Wait(timer.RemainingTime.ToMillisecondsTimeout(), cancelToken))
                    {
                        confirmEvent.Dispose();
                    }
                    else
                    {
                        Interlocked.Exchange(ref disposeEvent, 1);
                        throw new TimeoutException(string.Format("Failed to publish a message to the remote queue [{0}] on the exchange [{2}] within the time limit of {1}.", queueName, timeout, exchange));
                    }
                    if (error != null)
                    {
                        throw error;
                    }
                    Debug.WriteLine("{0}-{1}: Published message to queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, queueName);
                }
                finally
                {
                    model.BasicAcks -= ackCallback;
                    model.BasicReturn -= returnCallback;
                    model.BasicNacks -= nackCallback;
                }
            }, timer.RemainingTime, cancelToken);
        }

        private void TryCloseQueue()
        {
            if (!_deleteQueueOnDispose || (_modelAndConnection == null))
            {
                return;
            }
            try
            {
                MethodInvocationTrace.Write();
                _modelAndConnection.Item1.QueueDeleteNoWait(_queueName, false, false);
            }
            catch (Exception e)
            {
                Trace.TraceWarning("[{2}] Failed to delete the queue [{0}]. {1}", _queueName, e, GetType());
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _disposeRequested = true;
                lock (_modelLock)
                {
                    TryCloseQueue();
                    DisposeModelAndConnection(ref _modelAndConnection);
                }
            }
        }

        public void Dispose()
        {
            MethodInvocationTrace.Write();
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}