using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class SelfHostedService<TServiceContract, TService>
    {
        private sealed class InstanceContextProvider : IEndpointBehavior, IInstanceContextProvider
        {
            private readonly object _instance;

            public InstanceContextProvider(object instance)
            {
                _instance = instance;
            }

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {
            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {
            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                endpointDispatcher.DispatchRuntime.InstanceContextProvider = this;
            }

            public InstanceContext GetExistingInstanceContext(Message message, IContextChannel channel)
            {
                return new InstanceContext(_instance);
            }

            public void InitializeInstanceContext(InstanceContext instanceContext, Message message, IContextChannel channel)
            {
            }

            public bool IsIdle(InstanceContext instanceContext)
            {
                return true;
            }

            public void NotifyIdle(InstanceContextIdleCallback callback, InstanceContext instanceContext)
            {
            }

            public void Validate(ServiceEndpoint endpoint)
            {
            }
        }
    }
}