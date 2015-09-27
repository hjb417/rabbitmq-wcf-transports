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

        //TODO: Add support for delete on close
        public RabbitMQReaderConnection(IConnectionFactory connectionFactory, string exchange, string queueName, bool isDurable, bool deleteQueueOnDispose, TimeSpan? queueTimeToLive)
            : base(connectionFactory, queueName, deleteQueueOnDispose)
        {
            _exchange = exchange;
            _queueName = queueName;
            _isDurable = isDurable;
            _queueTtl = queueTimeToLive;
        }

        protected override void InitializeModel(IModel model)
        {
            model.BasicQos(0, 1, false);
            model.ConfirmSelect();
            var args = new Dictionary<string, object>();
            args.Add(ReaderQueueArguments.CommandLine, Environment.CommandLine);
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
            if (_isDurable)
            {
                model.QueueDeclare(_queueName, true, false, false, args);
            }
            else
            {
                model.QueueDeclare(_queueName, true, false, true, args);
            }
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

        public void BasicAck(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken)
        {
            MethodInvocationTrace.Write();
            PerformAction(model => model.BasicAck(deliveryTag, false), timeout, cancelToken);
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
    }
}