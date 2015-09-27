using System;
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices.VanillaService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class VanillaService : MarshalByRefObject, IVanillaService
    {
        public VanillaService()
        {
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public string Fail(string exceptionMessage)
        {
            throw new Exception(exceptionMessage);
        }

        public void FailOneWay(string exceptionMessage)
        {
            throw new Exception(exceptionMessage);
        }

        public string Success()
        {
            return Guid.NewGuid().ToString();
        }

        public void SuccessOneWay()
        {
        }
    }
}