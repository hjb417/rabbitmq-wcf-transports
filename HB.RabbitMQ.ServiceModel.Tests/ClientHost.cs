using System;
using System.Collections.Concurrent;
using System.Linq;
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public abstract class ClientHost<TServiceContract> : MarshalByRefObject
        where TServiceContract : class
    {
        private readonly ConcurrentBag<ICommunicationObject> _commObjs = new ConcurrentBag<ICommunicationObject>();
        private readonly ClientBaseFactory.ClientBase<TServiceContract> _host;

        protected ClientHost(BindingTypes bindingType, Uri serviceUri, Uri clientBaseAddress)
        {
            var binding = BindingFactory.Create(bindingType, _commObjs.Add, clientBaseAddress);
            _host = ClientBaseFactory.Create<TServiceContract>(binding, serviceUri.ToString());
            ServiceAppDomain = AppDomain.CurrentDomain;
        }

        protected ICommunicationObject Host { get { return _host; } }
        protected TServiceContract Service { get { return _host.Channel; } }
        public AppDomain ServiceAppDomain { get; private set; }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public CommunicationStateInfo[] GetStates<T>()
            where T : ICommunicationObject
        {
            return _commObjs.OfType<T>().Select(c => new CommunicationStateInfo(c.GetType(), c.State)).ToArray();
        }

        public CommunicationStateInfo[] GetStates()
        {
            return _commObjs.Select(c => new CommunicationStateInfo(c.GetType(), c.State)).ToArray();
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