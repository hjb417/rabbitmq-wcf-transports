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
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    internal sealed class RabbitMQReaderConnection : RabbitMQConnectionBase
    {
        private static readonly Process _proc = Process.GetCurrentProcess();
        private readonly string _exchange;
        private readonly string _queueName;
        private readonly bool _isDurable;
        private readonly TimeSpan? _queueTtl;
        private readonly RabbitMQReaderOptions _options;
        private readonly int? _maxPriority;

        public RabbitMQReaderConnection(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnDispose, TimeSpan? queueTimeToLive, RabbitMQReaderOptions options, int? maxPriority)
            : base(connectionFactory, queueName, deleteQueueOnDispose)
        {
            _options = options;
            _exchange = exchange;
            _queueName = queueName;
            _isDurable = isDurable;
            _queueTtl = queueTimeToLive;
            _maxPriority = maxPriority;
        }

        protected override void InitializeModel(IModel model)
        {
            model.BasicQos(0, 1, false);
            model.ConfirmSelect();
            var args = new Dictionary<string, object>();
            if (_options.IncludeProcessCommandLineInQueueArguments)
            {
                args.Add(ReaderQueueArguments.CommandLine, Environment.CommandLine);
            }
            args.Add(ReaderQueueArguments.ProcessStartTime, ((DateTimeOffset)_proc.StartTime).ToString());
            args.Add(ReaderQueueArguments.ProcessId, _proc.Id);
            args.Add(ReaderQueueArguments.MachineName, Environment.MachineName);
            args.Add(ReaderQueueArguments.CreationTime, DateTimeOffset.Now.ToString());
            args.Add(ReaderQueueArguments.UserName, Environment.UserName);
            args.Add(ReaderQueueArguments.UserDomainName, Environment.UserDomainName);
            args.Add(ReaderQueueArguments.AppDomainFriendlyName, AppDomain.CurrentDomain.FriendlyName);
            args.Add(ReaderQueueArguments.AppDomainFriendlId, AppDomain.CurrentDomain.Id);
#if DEBUG
            args.Add(ReaderQueueArguments.Stacktrace, Environment.StackTrace);
#endif

            var appIdentity = AppDomain.CurrentDomain.ApplicationIdentity;
            args.Add(ReaderQueueArguments.ApplicationIdentity, (appIdentity == null) ? string.Empty : appIdentity.ToString());
            if (_queueTtl.HasValue)
            {
                args.Add("x-expires", (int)_queueTtl.Value.TotalMilliseconds);
            }
            if(_maxPriority.HasValue)
            {
                args.Add("x-max-priority", _maxPriority.Value);
            }
            model.QueueDeclare(_queueName, true, false, !_isDurable, args);
            Debug.WriteLine("{0}-{1}: Declared queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, _queueName);
            model.QueueBind(_queueName, _exchange, _queueName);
            Debug.WriteLine("{0}-{1}: Bound to queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, _queueName);
        }

        public BasicGetResult BasicGet(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            return PerformAction(model => model.BasicGet(_queueName, false), timeout, cancelToken);
        }

        public IBasicProperties CreateBasicProperties(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            return PerformAction(model => model.CreateBasicProperties(), timeout, cancelToken);
        }

        public void BasicReject(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            PerformAction(model => model.BasicReject(deliveryTag, false), timeout, cancelToken);
        }

        public QueueDeclareOk QueueDeclarePassive(TimeSpan timeout, CancellationToken cancelToken)
        {
            return PerformAction(model => model.QueueDeclarePassive(_queueName), timeout, cancelToken);
        }

        public uint MessageCount(TimeSpan timeout, CancellationToken cancelToken)
        {
            return PerformAction(model => model.MessageCount(_queueName), timeout, cancelToken);
        }
    }
}