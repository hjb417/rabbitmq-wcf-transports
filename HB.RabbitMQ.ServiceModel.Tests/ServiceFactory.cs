using System;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class ServiceFactory
    {
        public static SelfHostedService<TServiceContract, TService> CreateServer<TServiceContract, TService>(BindingTypes bindingType)
            where TServiceContract : class
            where TService : TServiceContract, new()
        {
            var appDomain = AppDomain.CurrentDomain.Clone();
            return appDomain.CreateInstanceAndUnwrap<SelfHostedService<TServiceContract, TService>>(bindingType);
        }

        public static TClient CreateClient<TClient>(BindingTypes bindingType, Uri serviceUri, Uri clientBaseAddress)
            where TClient : class
        {
            var appDomain = AppDomain.CurrentDomain.Clone();
            return appDomain.CreateInstanceAndUnwrap<TClient>(bindingType, serviceUri, clientBaseAddress);
        }
    }
}