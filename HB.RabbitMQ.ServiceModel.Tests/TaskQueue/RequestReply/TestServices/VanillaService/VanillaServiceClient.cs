using System;
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices.VanillaService
{
    public class VanillaServiceClient : ClientHost<IVanillaService>, IVanillaService
    {
        public VanillaServiceClient(BindingTypes bindingType, Uri serviceUri, Uri clientBaseAddress)
            : base(bindingType, serviceUri, clientBaseAddress)
        {
        }

        public string Fail(string exceptionMessage)
        {
            try
            {
                return Service.Fail(exceptionMessage);
            }
            catch(FaultException<ExceptionDetail> e)
            {
                throw new Exception(e.ToString());
            }
        }

        public void FailOneWay(string exceptionMessage)
        {
            Service.FailOneWay(exceptionMessage);
        }

        public void Noop()
        {
            Service.Noop();
        }

        public string Success()
        {
            return Service.Success();
        }

        public void SuccessOneWay()
        {
            Service.SuccessOneWay();
        }
    }
}
