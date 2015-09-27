using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Messaging;
using System.ServiceModel;
using System.ServiceModel.Description;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public partial class SelfHostedService<TServiceContract, TService> : MarshalByRefObject
        where TServiceContract : class
        where TService : TServiceContract, new()
    {
        private readonly ConcurrentBag<ICommunicationObject> _commObjs = new ConcurrentBag<ICommunicationObject>();

        public SelfHostedService(BindingTypes bindingType)
        {
            QueueName = typeof(TServiceContract).Name + "-" + Guid.NewGuid();
            string uri;
            var listenMode = ListenUriMode.Explicit;
            switch (bindingType)
            {
                case BindingTypes.Msmq:
                case BindingTypes.DuplexMsmq:
                    MessageQueue.Create(@".\private$\" + QueueName);
                    uri = string.Format("net.msmq://localhost/private/{0}", QueueName);
                    break;
                case BindingTypes.RabbitMQTaskQueue:
                    uri = RabbitMQTaskQueueUri.Create(QueueName, ttl: TimeSpan.FromMinutes(10)).ToString();
                    break;
                case BindingTypes.NetTcp:
                    listenMode = ListenUriMode.Unique;
                    uri = string.Format("net.tcp://localhost:0/{0}/", QueueName);
                    break;
                default:
                    throw new NotSupportedException();
            }
            Service = new TService();
            var host = new ServiceHost(typeof(TService));
            var ep = host.AddServiceEndpoint(typeof(TServiceContract), BindingFactory.Create(bindingType, _commObjs.Add, null), uri);
            ep.ListenUriMode = listenMode;
            ep.EndpointBehaviors.Add(new InstanceContextProvider(Service));
            ep.EndpointBehaviors.Add(new MessageInspector(_commObjs.Add));
            Host = host;
            host.Open();
            ServiceUri = host.ChannelDispatchers.Select(d => d.Listener.Uri).Single();
            _commObjs.Add(host.ChannelDispatchers.Single().Listener);
            ServiceAppDomain = AppDomain.CurrentDomain;
        }

        public TService Service { get; private set; }
        public AppDomain ServiceAppDomain { get; private set; }
        protected ServiceHost Host { get; private set; }
        public Uri ServiceUri { get; private set; }
        public string QueueName { get; private set; }
        public string RabbitMQTaskQueueUritMqT { get; private set; }

        public CommunicationStateInfo[] GetStates<T>()
            where T : ICommunicationObject
        {
            return _commObjs.OfType<T>().Select(c => new CommunicationStateInfo(c.GetType(), c.State)).ToArray();
        }

        public CommunicationStateInfo[] GetStates()
        {
            return _commObjs.Select(c => new CommunicationStateInfo(c.GetType(), c.State)).ToArray();
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Close(TimeSpan timeout)
        {
            Host.Close(timeout);
        }

        public void Abort()
        {
            Host.Abort();
        }
    }
}