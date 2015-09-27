
namespace HB.RabbitMQ.ServiceModel
{
    internal static class MessageHeaders
    {
        private const string ArgumentsPrefix = "hb.rmq.sm-";
        public const string CommandLine = ArgumentsPrefix + "CommandLine";
        public const string ProcessStartTime = ArgumentsPrefix + "ProcessStartTime";
        public const string ProcessId = ArgumentsPrefix + "ProcessId";
        public const string MachineName = ArgumentsPrefix + "MachineName";
        public const string CreationTime = ArgumentsPrefix + "CreationTime";
        public const string UserName = ArgumentsPrefix + "UserName";
        public const string UserDomainName = ArgumentsPrefix + "UserDomainName";
        public const string AppDomainFriendlyName = ArgumentsPrefix + "AppDomainFriendlyName";
        public const string AppDomainFriendlId = ArgumentsPrefix + "AppDomainFriendlId";
        public const string ApplicationIdentity = ArgumentsPrefix + "ApplicationIdentity";
    }
}