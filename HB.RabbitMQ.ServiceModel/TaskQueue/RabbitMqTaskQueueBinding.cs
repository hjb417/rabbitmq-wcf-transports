using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    //TODO: Add DefaultTimeToLive
    public sealed class RabbitMQTaskQueueBinding : CustomBinding
    {
        private RabbitMQTransportBindingElement _transport;

        public RabbitMQTaskQueueBinding()
        {
            Initialize();
        }

        public RabbitMQTaskQueueBinding(string configurationName)
            : base(configurationName)
        {
            Initialize();
        }

        public long MaxBufferPoolSize { get { return _transport.MaxBufferPoolSize; } set { _transport.MaxBufferPoolSize = value; } }
        public long MaxReceivedMessageSize { get { return _transport.MaxReceivedMessageSize; } set { _transport.MaxReceivedMessageSize = value; } }
        public IConnectionFactory ConnectionFactory { get; set; }
        public IDequeueThrottlerFactory DequeueThrottlerFactory { get; set; }
        internal IRabbitMQReaderWriterFactory QueueReaderWriterFactory { get; set; }
        internal Action<ICommunicationObject> CommunicationObjectCreatedCallback { get; set; }
        internal RabbitMQReaderOptions ReaderOptions { get; private set; }
        internal RabbitMQWriterOptions WriterOptions { get; private set; }
        public TimeSpan? QueueTimeToLive { get; set; }

        public bool IncludeProcessCommandLineInQueueArguments
        {
            get { return ReaderOptions.IncludeProcessCommandLineInQueueArguments; }
            set { ReaderOptions.IncludeProcessCommandLineInQueueArguments = value; }
        }

        public bool IncludeProcessCommandLineInMessageHeaders
        {
            get { return WriterOptions.IncludeProcessCommandLineInMessageHeaders; }
            set { WriterOptions.IncludeProcessCommandLineInMessageHeaders = value; }
        }

        public override string Scheme { get { return _transport.Scheme; } }
        
        internal BufferManager CreateBufferManager()
        {
            return BufferManager.CreateBufferManager(MaxBufferPoolSize, (int)MaxReceivedMessageSize);
        }

        private void Initialize()
        {
            QueueReaderWriterFactory = RabbitMQReaderWriterFactory.Instance;
            DequeueThrottlerFactory = NoOpDequeueThrottlerFactory.Instance;
            QueueTimeToLive = TimeSpan.FromMinutes(20);
            ReaderOptions = new RabbitMQReaderOptions();
            WriterOptions = new RabbitMQWriterOptions();
            _transport = new RabbitMQTransportBindingElement(this);
        }

        public override BindingElementCollection CreateBindingElements()
        {
            var bindings = base.CreateBindingElements();
            bindings.Add(new TransactionFlowBindingElement() { AllowWildcardAction = true, TransactionProtocol = TransactionProtocol.OleTransactions });
            bindings.Add(new RabbitMQTransportBindingElement(this));
            return bindings;
        }
    }
}