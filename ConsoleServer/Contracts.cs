using System.ServiceModel;

namespace Contracts
{
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    //[ServiceContract()]
    public interface ISimpleService
    {
        [OperationContract]
        string Echo(string input);

        [OperationContract(IsOneWay = true)]
        void OneWayEcho(string input);
    }

    public interface ISimpleServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void Ack(string input);
    }
}