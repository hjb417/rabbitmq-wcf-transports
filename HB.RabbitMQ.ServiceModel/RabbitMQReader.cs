using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal class RabbitMQReader : IRabbitMQReader
    {
        private readonly ConcurrentOperationManager _invocationTracker;
        private volatile bool _isDisposed;
        private bool _deleteQueue;
        private readonly IDequeueThrottler _throttler;
        private static readonly TimeSpan _keepAliveInterval = TimeSpan.FromMinutes(5);
        private static readonly byte[] _emptyBuffer = new byte[0];
        private readonly RabbitMQReaderConnection _conn;
        private volatile bool _softCloseRequested;

        public RabbitMQReader(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnClose, TimeSpan? queueTimeToLive, IDequeueThrottler throttler, RabbitMQReaderOptions options)
        {
            MethodInvocationTrace.Write();
            QueueName = queueName;
            Exchange = exchange;
            _invocationTracker = new ConcurrentOperationManager(GetType().FullName);
            _conn = new RabbitMQReaderConnection(connectionFactory, exchange, queueName, isDurable, deleteQueueOnClose, queueTimeToLive, options);
            _deleteQueue = !isDurable;
            _throttler = throttler;
        }

        [ExcludeFromCodeCoverage]
        ~RabbitMQReader()
        {
            Dispose(false);
        }

        public string QueueName { get; private set; }
        public string Exchange { get; private set; }

        public void EnsureOpen(TimeSpan timeout, CancellationToken cancelToken)
        {
            if (_softCloseRequested)
            {
                return;
            }
            using (_invocationTracker.TrackOperation())
            using (var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _invocationTracker.Token))
            {
                _conn.EnsureConnectionOpen(timeout, cancelTokenSource.Token);
            }
        }

        public QueueDeclareOk QueryQueue(TimeSpan timeout, CancellationToken cancelToken)
        {
            if (_softCloseRequested)
            {
                return new QueueDeclareOk(QueueName, 0, 0);
            }
            //MethodInvocationTrace.Write();
            return _conn.QueueDeclarePassive(timeout, cancelToken);
        }

        public bool WaitForMessage(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (_invocationTracker.TrackOperation())
            using (var opCancelToken = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _invocationTracker.Token))
            {
                while (true)
                {
                    if (!timeoutTimer.HasTimeRemaining)
                    {
                        return false;
                    }
                    if (opCancelToken.Token.IsCancellationRequested)
                    {
                        return false;
                    }
                    try
                    {
                        var queueInfo = QueryQueue(timeoutTimer.RemainingTime, opCancelToken.Token);
                        if (queueInfo.MessageCount > 0)
                        {
                            return true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return false;
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                }
            }
        }

        public DequeueResult Dequeue(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            using (_invocationTracker.TrackOperation())
            using (var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _invocationTracker.Token))
            {
                try
                {
                    cancelTokenSource.CancelAfter(timeoutTimer.RemainingTime);
                    while (true)
                    {
                        if (_isDisposed)
                        {
                            throw new ObjectDisposedException(GetType().FullName);
                        }
                        timeoutTimer.ThrowIfNoTimeRemaining();
                        cancelTokenSource.Token.ThrowIfCancellationRequested();
                        var queueInfo = QueryQueue(timeoutTimer.RemainingTime, cancelTokenSource.Token);
                        if (queueInfo.MessageCount > 0)
                        {
                            var throttleResult = _throttler.Throttle(queueInfo.MessageCount, queueInfo.ConsumerCount, cancelTokenSource.Token);
                            if (throttleResult == ThrottleResult.SkipMessage)
                            {
                                continue;
                            }
                            var msg = _conn.BasicGet(timeoutTimer.RemainingTime, cancelTokenSource.Token);
                            if (msg != null)
                            {
                                var body = msg.Body;
                                Stream messageBufferStream = null;
                                try
                                {
                                    messageBufferStream = new MemoryStream(msg.Body);
                                    return new DequeueResult(msg.DeliveryTag, msg.Redelivered, msg.Exchange, msg.RoutingKey, msg.MessageCount, msg.BasicProperties, messageBufferStream);
                                }
                                catch
                                {
                                    DisposeHelper.DisposeIfNotNull(messageBufferStream);
                                    throw;
                                }
                            }
                        }
                        Thread.Sleep(1);
                    }
                }
                catch (OperationCanceledException e)
                {
                    if (_isDisposed)
                    {
                        throw new ObjectDisposedException(GetType().FullName, e);
                    }
                    throw;
                }
            }
        }

        public void AcknowledgeMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            using (_invocationTracker.TrackOperation())
            using (var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _invocationTracker.Token))
            {
                _conn.BasicAck(deliveryTag, timeout, cancelTokenSource.Token);
            }
        }

        public void RejectMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            using (_invocationTracker.TrackOperation())
            using (var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, _invocationTracker.Token))
            {
                _conn.BasicReject(deliveryTag, timeout, cancelTokenSource.Token);
            }
        }

        public void SoftClose()
        {
            MethodInvocationTrace.Write();
            _softCloseRequested = true;
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    Dispose();
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("[{2}] Failed to dispose object of type [{0}]. {1}", GetType(), e, GetType());
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                MethodInvocationTrace.Write();
                _isDisposed = true;
                _invocationTracker.Dispose();
                _conn.Dispose();
                _throttler.Dispose();
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