namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal static class BindingPropertyNames
    {
        public const string HostName = "hostName";
        public const string Port = "port";
        public const string Password = "password";
        public const string UserName = "username";
        public const string VirtualHost = "virtualHost";
        public const string Protocol = "protocol";
        public const string DequeueThrottlerFactory = "dequeueThrottlerFactory";
        public const string MaxBufferPoolSize = "maxBufferPoolSize";
        public const string MaxReceivedMessageSize = "maxReceivedMessageSize";
        public const string QueueTimeToLive = "queueTimeToLive";
        public const string IncludeProcessCommandLineInQueueArguments = "includeProcessCommandLineInQueueArguments";
        public const string IncludeProcessCommandLineInMessageHeaders = "includeProcessCommandLineInMessageHeaders";
    }
}