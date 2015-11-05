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
using System.Configuration;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed partial class RabbitMQTaskQueueBindingElement : StandardBindingElement
    {
        public RabbitMQTaskQueueBindingElement()
        {
        }

        [ConfigurationProperty(BindingPropertyNames.WriterOptions, DefaultValue = DefaultValues.WriterOptions)]
        public RabbitMQWriterOptionsBindingElement WriterOptions
        {
            get { return ((RabbitMQWriterOptionsBindingElement)base[BindingPropertyNames.WriterOptions]); }
        }

        [ConfigurationProperty(BindingPropertyNames.ReaderOptions, DefaultValue = DefaultValues.ReaderOptions)]
        public RabbitMQReaderOptionsBindingElement ReaderOptions
        {
            get { return ((RabbitMQReaderOptionsBindingElement)base[BindingPropertyNames.ReaderOptions]); }
        }

        [ConfigurationProperty(BindingPropertyNames.HostName, DefaultValue = DefaultValues.HostName)]
        public string HostName
        {
            get { return ((string)base[BindingPropertyNames.HostName]); }
            set { base[BindingPropertyNames.HostName] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.Port, DefaultValue = DefaultValues.Port)]
        public int Port
        {
            get { return ((int)base[BindingPropertyNames.Port]); }
            set { base[BindingPropertyNames.Port] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.MaxBufferPoolSize, DefaultValue = DefaultValues.MaxBufferPoolSize)]
        public long MaxBufferPoolSize
        {
            get { return ((long)base[BindingPropertyNames.MaxBufferPoolSize]); }
            set { base[BindingPropertyNames.MaxBufferPoolSize] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.MaxReceivedMessageSize, DefaultValue = DefaultValues.MaxReceivedMessageSize)]
        public long MaxReceivedMessageSize
        {
            get { return ((long)base[BindingPropertyNames.MaxReceivedMessageSize]); }
            set { base[BindingPropertyNames.MaxReceivedMessageSize] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.AutoCreateServerQueue, DefaultValue = DefaultValues.AutoCreateServerQueue)]
        public bool AutoCreateServerQueue
        {
            get { return ((bool)base[BindingPropertyNames.AutoCreateServerQueue]); }
            set { base[BindingPropertyNames.AutoCreateServerQueue] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.QueueTimeToLive, DefaultValue = DefaultValues.QueueTimeToLive)]
        public TimeSpan? QueueTimeToLive
        {
            get { return ((TimeSpan?)base[BindingPropertyNames.QueueTimeToLive]); }
            set { base[BindingPropertyNames.QueueTimeToLive] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.MessageConfirmationMode, DefaultValue = MessageConfirmationModes.BeforeReply)]
        public MessageConfirmationModes MessageConfirmationMode
        {
            get { return ((MessageConfirmationModes)base[BindingPropertyNames.MessageConfirmationMode]); }
            set { base[BindingPropertyNames.MessageConfirmationMode] = value; }
        }        

        [ConfigurationProperty(BindingPropertyNames.Password, DefaultValue = DefaultValues.Password)]
        public string Password
        {
            get { return ((string)base[BindingPropertyNames.Password]); }
            set { base[BindingPropertyNames.Password] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.UserName, DefaultValue = DefaultValues.Username)]
        public string Username
        {
            get { return ((string)base[BindingPropertyNames.UserName]); }
            set { base[BindingPropertyNames.UserName] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.VirtualHost, DefaultValue = DefaultValues.VirtualHost)]
        public string VirtualHost
        {
            get { return ((string)base[BindingPropertyNames.VirtualHost]); }
            set { base[BindingPropertyNames.VirtualHost] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.Protocol, DefaultValue = DefaultValues.Protocol)]
        public string Protocol
        {
            get { return ((string)base[BindingPropertyNames.Protocol]); }
            set { base[BindingPropertyNames.Protocol] = value; }
        }

        protected override Type BindingElementType
        {
            get { return typeof(RabbitMQTaskQueueBinding); }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get
            {
                ConfigurationPropertyCollection configProperties = base.Properties;
                foreach (var prop in GetType().GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance))
                {
                    foreach (ConfigurationPropertyAttribute attr in prop.GetCustomAttributes(typeof(ConfigurationPropertyAttribute), false))
                    {
                        configProperties.Add(new ConfigurationProperty(attr.Name, prop.PropertyType, attr.DefaultValue));
                    }
                } return configProperties;
            }
        }

        protected override void OnApplyConfiguration(Binding binding)
        {
            var rb = (RabbitMQTaskQueueBinding)binding;
            rb.MaxReceivedMessageSize = MaxReceivedMessageSize;
            rb.MaxBufferPoolSize = MaxBufferPoolSize;
            rb.QueueTimeToLive = QueueTimeToLive;
            rb.MessageConfirmationMode = MessageConfirmationMode;
            rb.AutoCreateServerQueue = AutoCreateServerQueue;
            rb.ConnectionFactory = new ConnectionFactory
            {
                HostName = HostName,
                Port = Port,
                Protocol = GetRabbitMQProtocol(),
                UserName = Username,
                Password = Password,
                VirtualHost = VirtualHost,
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = 180,
                UseBackgroundThreadsForIO = true,
            };
            WriterOptions.ApplyConfiguration(rb.WriterOptions);
            ReaderOptions.ApplyConfiguration(rb.ReaderOptions);
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
