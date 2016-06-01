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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal class RabbitMQWriter : IRabbitMQWriter
    {
        private readonly RabbitMQWriterConnection _conn;
        private readonly ConcurrentOperationManager _invocationTracker;
        private static readonly Process _proc = Process.GetCurrentProcess();
        private readonly RabbitMQWriterSetup _setup;

        public RabbitMQWriter(RabbitMQWriterSetup setup, bool cloneSetup)
        {
            _setup = cloneSetup ? setup.Clone() : setup;
            MethodInvocationTrace.Write();
            _invocationTracker = new ConcurrentOperationManager(GetType().FullName);
            _conn = new RabbitMQWriterConnection(_setup.ConnectionFactory);
        }

        [ExcludeFromCodeCoverage]
        ~RabbitMQWriter()
        {
            Dispose(false);
        }

        public void EnsureOpen(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            using (_invocationTracker.TrackOperation())
            using (var opCancelToken = CancellationTokenSource.CreateLinkedTokenSource(_invocationTracker.Token, cancelToken))
            {
                _conn.EnsureConnectionOpen(timeout, opCancelToken.Token);
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

        public void Enqueue(string exchange, string queueName, Stream messageStream, TimeSpan timetoLive, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            using (_invocationTracker.TrackOperation())
            using (var opCancelToken = CancellationTokenSource.CreateLinkedTokenSource(_invocationTracker.Token, cancelToken))
            {
                var timeoutTimer = TimeoutTimer.StartNew(timeout);
                var msgProps = _conn.CreateBasicProperties(timeoutTimer.RemainingTime, opCancelToken.Token);

                msgProps.Headers = new Dictionary<string, object>();
                if (_setup.Options.IncludeProcessCommandLineInMessageHeaders)
                {
                    msgProps.Headers.Add(MessageHeaders.CommandLine, Environment.CommandLine);
                }
                msgProps.Headers.Add(MessageHeaders.ProcessStartTime, ((DateTimeOffset)_proc.StartTime).ToString());
                msgProps.Headers.Add(MessageHeaders.ProcessId, _proc.Id);
                msgProps.Headers.Add(MessageHeaders.MachineName, Environment.MachineName);
                msgProps.Headers.Add(MessageHeaders.CreationTime, DateTimeOffset.Now.ToString());
                msgProps.Headers.Add(MessageHeaders.UserName, Environment.UserName);
                msgProps.Headers.Add(MessageHeaders.UserDomainName, Environment.UserDomainName);
                msgProps.Headers.Add(MessageHeaders.AppDomainFriendlyName, AppDomain.CurrentDomain.FriendlyName);
                msgProps.Headers.Add(MessageHeaders.AppDomainFriendlId, AppDomain.CurrentDomain.Id);

                msgProps.Persistent = true;
                if (timetoLive != TimeSpan.MaxValue)
                {
                    msgProps.Expiration = timetoLive.TotalMilliseconds.ToString("0");
                }
                if (_setup.Options.MessagePriority.HasValue)
                {
                    msgProps.Priority = (byte)_setup.Options.MessagePriority.Value;
                }
                _conn.BasicPublish(exchange, queueName, msgProps, messageStream, timeoutTimer.RemainingTime, opCancelToken.Token);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _invocationTracker.Dispose();
                _conn.Dispose();
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