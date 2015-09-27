using HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices.VanillaService;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.RequestReply.TestServices
{
    public abstract class VanillaServiceTests : UnitTest
    {
        private bool _unloadClientAppDomain = true;
        private bool _unloadServerAppDomain = true;

        protected VanillaServiceTests(BindingTypes bindingType)
        {
            Server = ServiceFactory.CreateServer<IVanillaService, VanillaService.VanillaService>(bindingType);
            Client = ServiceFactory.CreateClient<VanillaServiceClient>(bindingType, Server.ServiceUri, null);
        }

        protected SelfHostedService<IVanillaService, VanillaService.VanillaService> Server { get; private set; }
        protected VanillaServiceClient Client { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_unloadServerAppDomain)
                {
                    try
                    {
                        Server.ServiceAppDomain.Unload();
                    }
                    catch { }
                }
                if (_unloadClientAppDomain)
                {
                    try
                    {
                        Client.ServiceAppDomain.Unload();
                    }
                    catch { }
                }
            }
            base.Dispose(disposing);
        }

    }
}
