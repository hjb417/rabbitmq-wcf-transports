using System;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    partial class SelfHostedService<TServiceContract, TService>
    {
        private sealed class MessageInspector : IEndpointBehavior, IDispatchMessageInspector
        {
            private readonly Action<ICommunicationObject> _addCallback;

            public MessageInspector(Action<ICommunicationObject> addCallback)
            {
                _addCallback = addCallback;
            }

            public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
            {

            }

            public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
            {

            }

            public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
            {
                endpointDispatcher.DispatchRuntime.MessageInspectors.Add(this);
            }

            public void Validate(ServiceEndpoint endpoint)
            {

            }

            public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
            {
                //HACK: Get the inner channel.
                var innerChannel = channel.GetType().GetProperty("InnerChannel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(channel, null);
                _addCallback((ICommunicationObject)innerChannel);
                return null;
            }

            public void BeforeSendReply(ref Message reply, object correlationState)
            {
            }
        }
    }
}
