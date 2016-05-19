using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using HB.RabbitMQ.ServiceModel.TaskQueue;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public static partial class BindingFactory
    {
        public static Binding Create(BindingTypes bindingType, Action<ICommunicationObject> communicationObjectCreatedCallback, Uri clientBaseAddress)
        {
            Binding binding;
            switch (bindingType)
            {
                case BindingTypes.Msmq:
                    binding = CreateMsMqBinding();
                    break;
                case BindingTypes.DuplexMsmq:
                    binding = CreateDuplexMsMqBinding(clientBaseAddress);
                    break;
                case BindingTypes.NetTcp:
                    binding = CreateNetTcpBinding();
                    break;
                case BindingTypes.RabbitMQTaskQueue:
                    binding = CreateRabbitMQTaskQueueBinding(communicationObjectCreatedCallback);
                    break;
                default:
                    throw new NotSupportedException();
            }
            binding.OpenTimeout = TimeSpan.FromSeconds(15);
            return binding;
        }

        private static Binding CreateRabbitMQTaskQueueBinding(Action<ICommunicationObject> communicationObjectCreatedCallback)
        {
            return new RabbitMQTaskQueueBinding
            {
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                CloseTimeout = TimeSpan.MaxValue,
                TimeToLive = TimeSpan.FromMinutes(10),
                CommunicationObjectCreatedCallback = communicationObjectCreatedCallback,
            };
        }

        private static Binding CreateNetTcpBinding()
        {
            return new NetTcpBinding
            {
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                CloseTimeout = TimeSpan.MaxValue,
            };
        }

        private static Binding CreateMsMqBinding()
        {
            return new NetMsmqBinding
            {
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                CloseTimeout = TimeSpan.MaxValue,
                ExactlyOnce = false,
                Security = new NetMsmqSecurity { Mode = NetMsmqSecurityMode.None },
            };
        }

        private static Binding CreateDuplexMsMqBinding(Uri clientBaseAddress)
        {
            var msmqBinding = new MsmqTransportBindingElement();
            msmqBinding.MsmqTransportSecurity.MsmqAuthenticationMode = MsmqAuthenticationMode.None;
            msmqBinding.MsmqTransportSecurity.MsmqProtectionLevel = System.Net.Security.ProtectionLevel.None;
            msmqBinding.UseActiveDirectory = false;
            msmqBinding.ExactlyOnce = false;

            var bindingElements = new BindingElementCollection();
            bindingElements.Add(new ReliableSessionBindingElement(true));
            bindingElements.Add(new CompositeDuplexBindingElement());
            if (clientBaseAddress != null)
            {
                var listenUri = new ListenUriBindingElement
                {
                    ListenUriBaseAddress = clientBaseAddress,
                    ListenUriMode = ListenUriMode.Explicit,
                };
                bindingElements.Add(listenUri);
            }
            bindingElements.Add(msmqBinding);
            return new CustomBinding(bindingElements);
        }
    }
}