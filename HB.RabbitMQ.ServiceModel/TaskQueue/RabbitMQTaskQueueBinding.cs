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
using System.ServiceModel;
using System.ServiceModel.Channels;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed class RabbitMQTaskQueueBinding : CustomBinding
    {
        private RabbitMQTransportBindingElement _transport;
        private RabbitMQReaderOptions _rdrOptions = new RabbitMQReaderOptions();
        private RabbitMQWriterOptions _writerOptions = new RabbitMQWriterOptions();
        private TimeSpan? _ttl = TimeSpan.FromMinutes(20);
        private IRabbitMQReaderWriterFactory _queueRwFactory = RabbitMQReaderWriterFactory.Instance;
        private MessageConfirmationModes _confMode = MessageConfirmationModes.BeforeReply;

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
        internal Action<ICommunicationObject> CommunicationObjectCreatedCallback { get; set; }

        internal IRabbitMQReaderWriterFactory QueueReaderWriterFactory
        {
            get { return _queueRwFactory; }
            set { _queueRwFactory = value; }
        }

        public RabbitMQReaderOptions ReaderOptions
        {
            get { return _rdrOptions; }
            set { _rdrOptions = value.Clone() ?? new RabbitMQReaderOptions(); }
        }

        public RabbitMQWriterOptions WriterOptions
        {
            get { return _writerOptions; }
            set { _writerOptions = value.Clone() ?? new RabbitMQWriterOptions(); }
        }

        public TimeSpan? QueueTimeToLive
        {
            get { return _ttl; }
            set { _ttl = value; }
        }

        public MessageConfirmationModes MessageConfirmationMode
        {
            get { return _confMode; }
            set { _confMode = value; }
        }

        public bool AutoCreateServerQueue { get; set; }

        public override string Scheme { get { return _transport.Scheme; } }

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
    }
}
