using System;
using System.Configuration;
using System.Reflection;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using HB.RabbitMQ.ServiceModel.Throttling;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    public sealed class RabbitMQTaskQueueBindingElement : StandardBindingElement
    {
        public RabbitMQTaskQueueBindingElement()
        {
            MaxBufferPoolSize = 524288;
            MaxReceivedMessageSize = 524288;
            QueueTimeToLive = TimeSpan.FromMinutes(20);
        }

        [ConfigurationProperty(BindingPropertyNames.HostName, DefaultValue = "localhost")]
        public string HostName
        {
            get { return ((string)base[BindingPropertyNames.HostName]); }
            set { base[BindingPropertyNames.HostName] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.Port, DefaultValue = AmqpTcpEndpoint.UseDefaultPort)]
        public int Port
        {
            get { return ((int)base[BindingPropertyNames.Port]); }
            set { base[BindingPropertyNames.Port] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.MaxBufferPoolSize, DefaultValue = 524288L)]
        public long MaxBufferPoolSize
        {
            get { return ((long)base[BindingPropertyNames.MaxBufferPoolSize]); }
            set { base[BindingPropertyNames.MaxBufferPoolSize] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.MaxReceivedMessageSize, DefaultValue = 65536L)]
        public long MaxReceivedMessageSize
        {
            get { return ((long)base[BindingPropertyNames.MaxReceivedMessageSize]); }
            set { base[BindingPropertyNames.MaxReceivedMessageSize] = value; }
        }
        
        public TimeSpan? QueueTimeToLive
        {
            get { return ((TimeSpan?)base[BindingPropertyNames.QueueTimeToLive]); }
            set { base[BindingPropertyNames.QueueTimeToLive] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.Password, DefaultValue = ConnectionFactory.DefaultPass)]
        public string Password
        {
            get { return ((string)base[BindingPropertyNames.Password]); }
            set { base[BindingPropertyNames.Password] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.UserName, DefaultValue = ConnectionFactory.DefaultUser)]
        public string Username
        {
            get { return ((string)base[BindingPropertyNames.UserName]); }
            set { base[BindingPropertyNames.UserName] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.VirtualHost, DefaultValue = ConnectionFactory.DefaultVHost)]
        public string VirtualHost
        {
            get { return ((string)base[BindingPropertyNames.VirtualHost]); }
            set { base[BindingPropertyNames.VirtualHost] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.Protocol, DefaultValue = AmqpProtocols.Default)]
        public string Protocol
        {
            get { return ((string)base[BindingPropertyNames.Protocol]); }
            set { base[BindingPropertyNames.Protocol] = value; }
        }

        [ConfigurationProperty(BindingPropertyNames.DequeueThrottlerFactory)]
        public string DequeueThrottlerFactory
        {
            get { return ((string)base[BindingPropertyNames.DequeueThrottlerFactory]); }
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
                configProperties.Add(new ConfigurationProperty("QueueTimeToLive", typeof(TimeSpan?), TimeSpan.FromMinutes(20)));
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
            if (!string.IsNullOrEmpty(DequeueThrottlerFactory))
            {
                rb.DequeueThrottlerFactory = (IDequeueThrottlerFactory)Activator.CreateInstance(Type.GetType(DequeueThrottlerFactory));
            }
            rb.MaxReceivedMessageSize = MaxReceivedMessageSize;
            rb.MaxBufferPoolSize = MaxBufferPoolSize;
            rb.QueueTimeToLive = QueueTimeToLive;
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