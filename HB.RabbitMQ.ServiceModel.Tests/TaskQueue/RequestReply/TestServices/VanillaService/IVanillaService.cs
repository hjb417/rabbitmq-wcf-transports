using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices.VanillaService
{
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IVanillaService
    {
        [OperationContract(IsOneWay = true)]
        void FailOneWay(string exceptionMessage);

        [OperationContract]
        string Fail(string exceptionMessage);

        [OperationContract(IsOneWay = true)]
        void SuccessOneWay();

        [OperationContract]
        void Noop();

        [OperationContract]
        string Success();
    }
}
