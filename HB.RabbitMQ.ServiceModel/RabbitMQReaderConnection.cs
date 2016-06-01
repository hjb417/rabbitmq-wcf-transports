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
        private readonly RabbitMQReaderSetup _setup;

        private RabbitMQReaderConnection(RabbitMQReaderSetup setup)
            : base(setup.ConnectionFactory, setup.QueueName, setup.DeleteQueueOnClose)
        {
            _setup = setup;
        }

        public static RabbitMQReaderConnection Create(RabbitMQReaderSetup setup, bool cloneSetup)
        {
            return new RabbitMQReaderConnection(cloneSetup ? setup.Clone() : setup);
        }

        protected override void InitializeModel(IModel model)
        {
            model.BasicQos(0, 1, false);
            model.ConfirmSelect();
            var args = new Dictionary<string, object>();
            if (_setup.Options.IncludeProcessCommandLineInQueueArguments)
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

            var autoDeleteQueue = !_setup.IsDurable;
            var queueTtl = _setup.QueueTimeToLive;
            if (autoDeleteQueue && !queueTtl.HasValue)
            {
                queueTtl = TimeSpan.FromMinutes(20);
            }
            var appIdentity = AppDomain.CurrentDomain.ApplicationIdentity;
            args.Add(ReaderQueueArguments.ApplicationIdentity, (appIdentity == null) ? string.Empty : appIdentity.ToString());
            if (queueTtl.HasValue)
            {
                args.Add("x-expires", (int)queueTtl.Value.TotalMilliseconds);
            }
            if (_setup.MaxPriority.HasValue)
            {
                args.Add("x-max-priority", _setup.MaxPriority.Value);
            }
            if (_setup.QueueArguments != null)
            {
                foreach (var extraOptions in _setup.QueueArguments)
                {
                    args.Add(extraOptions.Key, extraOptions.Value);
                }
            }
            model.QueueDeclare(_setup.QueueName, true, false, autoDeleteQueue, args);
            Debug.WriteLine("{0}-{1}: Declared queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, _setup.QueueName);
            model.QueueBind(_setup.QueueName, _setup.Exchange, _setup.QueueName);
            Debug.WriteLine("{0}-{1}: Bound to queue [{2}]", DateTime.Now, Thread.CurrentThread.ManagedThreadId, _setup.QueueName);
        }

        public BasicGetResult BasicGet(TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            return PerformAction(model => model.BasicGet(_setup.QueueName, false), timeout, cancelToken);
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
            return PerformAction(model => model.QueueDeclarePassive(_setup.QueueName), timeout, cancelToken);
        }

        public uint MessageCount(TimeSpan timeout, CancellationToken cancelToken)
        {
            return PerformAction(model => model.MessageCount(_setup.QueueName), timeout, cancelToken);
        }
    }
}