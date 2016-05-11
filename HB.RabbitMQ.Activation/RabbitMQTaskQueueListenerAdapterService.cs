/*
Copyright (c) 2015 HJB417

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.ComponentModel;
using System.Configuration;
using System.ServiceProcess;
using System.Threading;
using HB.RabbitMQ.Activation.Configuration;
using HB.RabbitMQ.Activation.Runtime.InteropServices;
using HB.RabbitMQ.ServiceModel.TaskQueue.Activation;
using RabbitMQ.Client;

namespace HB.RabbitMQ.Activation
{
    public partial class RabbitMQTaskQueueListenerAdapterService : ServiceBase
    {
        private IDisposable _adapter;

        public RabbitMQTaskQueueListenerAdapterService()
        {
            InitializeComponent();
        }

        public RabbitMQTaskQueueListenerAdapterService(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var adapter = (RabbitMQTaskQueueListenerAdapterSection)config.GetSection("rabbitMQTaskQueueListenerAdapter");
            var conn = adapter.Connection;

            var connFactory = new ConnectionFactory
            {
                HostName = conn.HostName,
                UserName = conn.UserName,
                Password = conn.Password,
                UseBackgroundThreadsForIO = true,
                RequestedHeartbeat = (ushort)conn.RequestedHeartbeat.TotalSeconds,
            };
            if (conn.AutomaticRecoveryEnabled.HasValue)
            {
                connFactory.AutomaticRecoveryEnabled = conn.AutomaticRecoveryEnabled.Value;
            }
            if (conn.TopologyRecoveryEnabled.HasValue)
            {
                connFactory.TopologyRecoveryEnabled = conn.TopologyRecoveryEnabled.Value;
            }
            if (conn.Port.HasValue)
            {
                connFactory.Port = conn.Port.Value;
            }

            RabbitMQTaskQueueListenerAdapter.InstallAdapter();
            _adapter = new RabbitMQTaskQueueListenerAdapter(connFactory);
        }

        protected override void OnStop()
        {
            _adapter.Dispose();
        }

        internal static void Run()
        {
            Run(new RabbitMQTaskQueueListenerAdapterService());
        }

        internal static void Run(string[] args, ManualResetEventSlim exitEvent)
        {
            using (var svc = new RabbitMQTaskQueueListenerAdapterService())
            {
                svc.OnStart(args);
                exitEvent.Wait();
                svc.OnStop();
            }
        }

        protected ServiceState State
        {
            get { return NativeMethods.QueryServiceStatus(ServiceHandle).CurrentState; }
            set
            {
                var status = new ServiceStatus
                {
                    CurrentState = value,
                    WaitHint = TimeSpan.FromSeconds(10),
                };
                NativeMethods.SetServiceStatus(ServiceHandle, ref status);
            }
        }
    }
}