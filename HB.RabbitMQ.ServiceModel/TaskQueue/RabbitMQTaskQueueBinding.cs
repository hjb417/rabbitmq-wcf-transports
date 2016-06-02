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
using System.ServiceModel;
using System.ServiceModel.Channels;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed class RabbitMQTaskQueueBinding : CustomBinding
    {
        private static readonly Process _proc = Process.GetCurrentProcess();
        private RabbitMQTransportBindingElement _transport;
        private RabbitMQReaderOptions _rdrOptions = new RabbitMQReaderOptions();
        private RabbitMQWriterOptions _writerOptions = new RabbitMQWriterOptions();
        private IRabbitMQReaderWriterFactory _queueRwFactory = RabbitMQReaderWriterFactory.Instance;
        private MessageConfirmationModes _confMode = MessageConfirmationModes.BeforeReply;
        private string _exchange = Constants.DefaultExchange;

        public RabbitMQTaskQueueBinding()
        {
            Initialize();
            new RabbitMQTaskQueueBindingElement().ApplyConfiguration(this);
        }

        public RabbitMQTaskQueueBinding(string configurationName)
            : base(configurationName)
        {
            Initialize();
        }

        public long MaxBufferPoolSize { get { return _transport.MaxBufferPoolSize; } set { _transport.MaxBufferPoolSize = value; } }
        public long MaxReceivedMessageSize { get { return _transport.MaxReceivedMessageSize; } set { _transport.MaxReceivedMessageSize = value; } }
        internal Action<ICommunicationObject> CommunicationObjectCreatedCallback { get; set; }

        internal IRabbitMQReaderWriterFactory QueueReaderWriterFactory
        {
            get { return _queueRwFactory; }
            set { _queueRwFactory = value; }
        }

        public RabbitMQReaderOptions ReaderOptions
        {
            get { return _rdrOptions; }
            set { _rdrOptions = (value == null) ? new RabbitMQReaderOptions() : value.Clone(); }
        }

        public RabbitMQWriterOptions WriterOptions
        {
            get { return _writerOptions; }
            set { _writerOptions = (value == null) ? new RabbitMQWriterOptions() : value.Clone(); }
        }

        public MessageConfirmationModes MessageConfirmationMode
        {
            get { return _confMode; }
            set { _confMode = value; }
        }

        public override string Scheme { get { return _transport.Scheme; } }

        public string Exchange
        {
            get { return _exchange; }
            set { _exchange = value ?? Constants.DefaultExchange; }

        }

        public bool IsDurable { get; set; }
        public bool DeleteOnClose { get; set; }
        public TimeSpan? TaskQueueTimeToLive { get; set; }
        public TimeSpan? ReplyQueueTimeToLive { get; set; }
        public int? MaxPriority { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string Protocol { get; set; }
        public bool AutomaticRecoveryEnabled { get; set; }
        public TimeSpan RequestedHeartbeat { get; set; }
        public bool UseBackgroundThreadsForIO { get; set; }

        internal BufferManager CreateBufferManager()
        {
            return BufferManager.CreateBufferManager(MaxBufferPoolSize, (int)MaxReceivedMessageSize);
        }

        private void Initialize()
        {
            _transport = new RabbitMQTransportBindingElement(this);
        }

        public override BindingElementCollection CreateBindingElements()
        {
            var bindings = base.CreateBindingElements();
            bindings.Add(new TransactionFlowBindingElement { AllowWildcardAction = true, TransactionProtocol = TransactionProtocol.OleTransactions });
            bindings.Add(new RabbitMQTransportBindingElement(this));
            return bindings;
        }

        public ConnectionFactory CreateConnectionFactory(string hostName, int port)
        {
            var connFactory = new ConnectionFactory
            {
                Protocol = GetRabbitMQProtocol(),
                HostName = hostName,
                Port = port,
                UserName = Username,
                Password = Password,
                VirtualHost = VirtualHost,
                AutomaticRecoveryEnabled = AutomaticRecoveryEnabled,
                RequestedHeartbeat = (ushort)RequestedHeartbeat.TotalSeconds,
                UseBackgroundThreadsForIO = UseBackgroundThreadsForIO,
            };
            connFactory.ClientProperties.Add(ReaderQueueArguments.ProcessStartTime, ((DateTimeOffset)_proc.StartTime).ToString());
            connFactory.ClientProperties.Add(ReaderQueueArguments.ProcessId, _proc.Id);
            connFactory.ClientProperties.Add(ReaderQueueArguments.MachineName, Environment.MachineName);
            connFactory.ClientProperties.Add(ReaderQueueArguments.CreationTime, DateTimeOffset.Now.ToString());
            connFactory.ClientProperties.Add(ReaderQueueArguments.UserName, Environment.UserName);
            connFactory.ClientProperties.Add(ReaderQueueArguments.UserDomainName, Environment.UserDomainName);
            connFactory.ClientProperties.Add(ReaderQueueArguments.AppDomainFriendlyName, AppDomain.CurrentDomain.FriendlyName);
            connFactory.ClientProperties.Add(ReaderQueueArguments.AppDomainFriendlId, AppDomain.CurrentDomain.Id);
#if DEBUG
            connFactory.ClientProperties.Add(ReaderQueueArguments.Stacktrace, Environment.StackTrace);
#endif
            if (ReaderOptions.IncludeProcessCommandLineInQueueArguments)
            {
                connFactory.ClientProperties.Add(ReaderQueueArguments.CommandLine, Environment.CommandLine);
            }
            return connFactory;
        }

        private IProtocol GetRabbitMQProtocol()
        {
            if (AmqpProtocols.Default.Equals(Protocol, StringComparison.OrdinalIgnoreCase))
            {
                return Protocols.DefaultProtocol;
            }
            if (AmqpProtocols.v0_9_1.Equals(Protocol, StringComparison.OrdinalIgnoreCase))
            {
                return Protocols.AMQP_0_9_1;
            }
            throw new NotSupportedException();
        }
    }
}
