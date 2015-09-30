using System;
using System.Configuration;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed partial class RabbitMQTaskQueueBindingElement : StandardBindingElement
    {
        public RabbitMQTaskQueueBindingElement()
        {
        }

        [ConfigurationProperty(BindingPropertyNames.IncludeProcessCommandLineInQueueArguments, DefaultValue = DefaultValues.IncludeProcessCommandLineInQueueArguments)]
        public bool IncludeProcessCommandLineInQueueArguments
        {
            get { return ((bool)base[BindingPropertyNames.IncludeProcessCommandLineInQueueArguments]); }
            set { base[BindingPropertyNames.IncludeProcessCommandLineInQueueArguments] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.IncludeProcessCommandLineInMessageHeaders, DefaultValue = DefaultValues.IncludeProcessCommandLineInMessageHeaders)]
        public bool IncludeProcessCommandLineInMessageHeaders
        {
            get { return ((bool)base[BindingPropertyNames.IncludeProcessCommandLineInMessageHeaders]); }
            set { base[BindingPropertyNames.IncludeProcessCommandLineInMessageHeaders] = value; }
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

        [ConfigurationProperty(BindingPropertyNames.QueueTimeToLive, DefaultValue = DefaultValues.QueueTimeToLive)]
        public TimeSpan? QueueTimeToLive
        {
            get { return ((TimeSpan?)base[BindingPropertyNames.QueueTimeToLive]); }
            set { base[BindingPropertyNames.QueueTimeToLive] = value; }
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

        [ConfigurationProperty(BindingPropertyNames.DequeueThrottlerFactory, DefaultValue = DefaultValues.DequeueThrottlerFactory)]
        public Type DequeueThrottlerFactory
        {
            get { return ((Type)base[BindingPropertyNames.DequeueThrottlerFactory]); }
            set { base[BindingPropertyNames.DequeueThrottlerFactory] = value; }
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
            if (DequeueThrottlerFactory != null)
            {
                rb.DequeueThrottlerFactory = (IDequeueThrottlerFactory)Activator.CreateInstance(DequeueThrottlerFactory);
            }
            rb.MaxReceivedMessageSize = MaxReceivedMessageSize;
            rb.MaxBufferPoolSize = MaxBufferPoolSize;
            rb.QueueTimeToLive = QueueTimeToLive;
            rb.IncludeProcessCommandLineInMessageHeaders = IncludeProcessCommandLineInMessageHeaders;
            rb.IncludeProcessCommandLineInQueueArguments = IncludeProcessCommandLineInQueueArguments;
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