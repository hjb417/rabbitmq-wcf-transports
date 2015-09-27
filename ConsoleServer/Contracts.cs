using System.ServiceModel;

namespace Contracts
{
    [ServiceContract(CallbackContract = typeof(ISimpleServiceCallback), SessionMode = SessionMode.Required)]
    //[ServiceContract()]
    public interface ISimpleService
    {
        [OperationContract]
        //[TransactionFlow(TransactionFlowOption.Allowed)]
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