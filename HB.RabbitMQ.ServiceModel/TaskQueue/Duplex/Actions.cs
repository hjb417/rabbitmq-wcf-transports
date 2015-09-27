
namespace HB.RabbitMQ.ServiceModel.TaskQueue.Duplex
{
    internal static class Actions
    {
        public const string CreateSessionRequest = "HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Actions.CreateSessionRequest";
        public const string CreateSessionResponse = "HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Actions.CreateSessionResponse";
        public const string CloseSessionRequest = "HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Actions.CloseSessionRequest";
        public const string InputSessionClosingRequest = "HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Actions.InputSessionClosingRequest";
        public const string KeepAlive = "HB.RabbitMQ.ServiceModel.TaskQueue.Duplex.Actions.KeepAlive";
    }
}