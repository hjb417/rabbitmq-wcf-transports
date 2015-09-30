using System;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    partial class RabbitMQTaskQueueBindingElement
    {
        internal sealed class DefaultValues
        {
            public const string HostName = "localhost";
            public const int Port = AmqpTcpEndpoint.UseDefaultPort;
            public const long MaxBufferPoolSize = 524288;
            public const long MaxReceivedMessageSize = 65536;
            public const string QueueTimeToLive = "00:20:00";
            public const string Password = ConnectionFactory.DefaultPass;
            public const string Username = ConnectionFactory.DefaultUser;
            public const string VirtualHost = ConnectionFactory.DefaultVHost;
            public const string Protocol = AmqpProtocols.Default;
            public const string DequeueThrottlerFactory = null;
            public const bool IncludeProcessCommandLineInQueueArguments = false;
            public const bool IncludeProcessCommandLineInMessageHeaders = false;
        }
    }
}