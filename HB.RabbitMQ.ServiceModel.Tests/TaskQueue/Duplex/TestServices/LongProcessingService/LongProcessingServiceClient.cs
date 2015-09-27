using System;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex.TestServices.LongProcessingService
{
    public class LongProcessingServiceClient : ClientHost<ILongProcessingService>, ILongProcessingService
    {
        public LongProcessingServiceClient(BindingTypes bindingType, Uri serviceUri, Uri clientBaseAddress)
            : base(bindingType, serviceUri, clientBaseAddress)
        {
        }

        public void Noop()
        {
            Service.Noop();
        }

        public void ProcessStuff(string stuff)
        {
            Service.ProcessStuff(stuff);
        }
    }
}