using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex.TestServices.LongProcessingService
{
    [ServiceContract(SessionMode = SessionMode.Required)]
    public interface ILongProcessingService
    {
        [OperationContract(IsOneWay = true)]
        void ProcessStuff(string stuff);

        [OperationContract(IsOneWay = true)]
        void Noop();
    }
}