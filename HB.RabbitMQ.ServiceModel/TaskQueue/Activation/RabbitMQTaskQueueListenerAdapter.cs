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
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter;
using HB.RabbitMQ.ServiceModel.Threading.Tasks.Schedulers;
using Microsoft.Web.Administration;
using RabbitMQ.Client;
using ListenerAdapter = HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter.ListenerAdapter;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    public partial class RabbitMQTaskQueueListenerAdapter : IDisposable
    {
        private const string ListenerAdapterPath = "system.applicationHost/listenerAdapters";
        private const string ProtocolsPath = "system.web/protocols";

        private readonly Dictionary<string, ApplicationInfo> _apps = new Dictionary<string, ApplicationInfo>();
        private readonly Dictionary<string, RabbitMQMessageListener> _rmqListeners = new Dictionary<string, RabbitMQMessageListener>();
        private readonly Dictionary<string, ApplicationPoolInfo> _appPools = new Dictionary<string, ApplicationPoolInfo>();
        private readonly ListenerAdapter _listenerAdapter;
        private readonly IConnection _connection;
        private readonly LimitedConcurrencyLevelTaskScheduler _taskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);
        private volatile bool _isDisposed;

        public RabbitMQTaskQueueListenerAdapter(IConnectionFactory connection)
        {
            _connection = connection.CreateConnection();
            _listenerAdapter = new ListenerAdapter(Constants.Scheme);
            _listenerAdapter.ApplicationCreated += ListenerAdapter_ApplicationCreated;
            _listenerAdapter.ApplicationDeleted += ListenerAdapter_ApplicationDeleted;
            _listenerAdapter.ApplicationRequestBlockedStateChanged += ListenerAdapter_ApplicationRequestBlockedStateChanged;

            _listenerAdapter.ApplicationPoolCanOpenNewListenerChannelInstance += ListenerAdapter_ApplicationPoolCanOpenNewListenerChannelInstance;
            _listenerAdapter.ApplicationPoolListenerChannelInstancesStopped += ListenerAdapter_ApplicationPoolListenerChannelInstancesStopped;
            _listenerAdapter.ApplicationPoolStateChanged += ListenerAdapter_ApplicationPoolStateChanged;
            _listenerAdapter.ApplicationAppPoolChanged += ListenerAdapter_ApplicationAppPoolChanged;
            _listenerAdapter.ApplicationPoolCreated += ListenerAdapter_ApplicationPoolCreated;
            _listenerAdapter.ApplicationPoolDeleted += ListenerAdapter_ApplicationPoolDeleted;

            _listenerAdapter.Initialize();
        }

        ~RabbitMQTaskQueueListenerAdapter()
        {
            Dispose(false);
        }

        private void ListenerAdapter_ApplicationPoolDeleted(object sender, ApplicationPoolDeletedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools => appPools.Remove(e.ApplicationPoolId));
        }

        private void ListenerAdapter_ApplicationPoolCreated(object sender, ApplicationPoolCreatedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools => appPools[e.ApplicationPoolName] = new ApplicationPoolInfo(e.ApplicationPoolName));
        }

        private void ListenerAdapter_ApplicationAppPoolChanged(object sender, ApplicationAppPoolChangedEventArgs e)
        {
            EnqueueApplicationAction(apps => apps[e.ApplicationName].UpdateApplicationPool(e.ApplicationPoolName, _appPools[e.ApplicationPoolName].State));
        }

        private void ListenerAdapter_ApplicationPoolStateChanged(object sender, ApplicationPoolStateChangedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools => appPools[e.ApplicationPoolId].State = e.State);
            EnqueueApplicationAction(apps =>
            {
                foreach(var app in apps.Values)
                {
                    if(app.ApplicationPoolName == e.ApplicationPoolId)
                    {
                        app.UpdateApplicationPool(e.ApplicationPoolId, e.State);
                    }
                }
            });
        }

        private void ListenerAdapter_ApplicationRequestBlockedStateChanged(object sender, ApplicationRequestBlockedStateChangedEventArgs e)
        {
            EnqueueApplicationAction(apps => apps[e.ApplicationName].RequestsBlockedState = e.State);
        }

        private void ListenerAdapter_ApplicationPoolListenerChannelInstancesStopped(object sender, ApplicationPoolListenerChannelInstanceEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = apps.Values
                .Where(a => a.ListenerChannelId.IsValueCreated)
                .Where(a => a.ListenerChannelId.Value == e.ListenerChannelId)
                .FirstOrDefault();
                if (app != null)
                {
                    app.RequestsBlockedState = ApplicationRequestsBlockedStates.Blocked;
                }
            });
        }

        private void ListenerAdapter_ApplicationDeleted(object sender, ApplicationDeletedEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = apps.Values.Where(a => a.ApplicationKey == e.ApplicationKey).FirstOrDefault();
                if (app != null)
                {
                    app.CanOpenNewListenerChannelInstance = false;
                    apps.Remove(app.ApplicationPath);
                }
            });
        }

        private void ListenerAdapter_ApplicationPoolCanOpenNewListenerChannelInstance(object sender, ApplicationPoolListenerChannelInstanceEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = apps.Values
                .Where(a => a.ListenerChannelId.IsValueCreated)
                .Where(a => a.ListenerChannelId.Value == e.ListenerChannelId)
                .FirstOrDefault();
                if (app != null)
                {
                    app.CanOpenNewListenerChannelInstance = true;
                }
            });
        }

        private void ListenerAdapter_ApplicationCreated(object sender, ApplicationCreatedEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = new ApplicationInfo(e.ApplicationKey, e.Url, e.SiteId, e.ApplicationPoolName, _appPools[e.ApplicationPoolName].State, e.RequestsBlockedState);
                new RabbitMQMessageListener(app, _connection).MessageQueuedEvent += delegate { OpenNewListenerChannelInstance(app); };
                apps.Add(e.Url, app);
            });
        }

        private void OpenNewListenerChannelInstance(ApplicationInfo app)
        {
            EnqueueAction(() =>
            {
                if (app.CanOpenNewListenerChannelInstance)
                {
                    app.CanOpenNewListenerChannelInstance = false;
                    if (!_isDisposed)
                    {
                        _listenerAdapter.OpenListenerChannelInstance(app.ApplicationPoolName, app.ListenerChannelId.Value, app.QueueBlob);
                    }
                }
            });
        }

        private void EnqueueAction(Action action)
        {
            Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, _taskScheduler);
        }

        private void EnqueueApplicationAction(Action<IDictionary<string, ApplicationInfo>> callback)
        {
            EnqueueAction(() => callback(_apps));
        }
        
        private void EnqueueApplcationPoolAction(Action<IDictionary<string, ApplicationPoolInfo>> callback)
        {
            EnqueueAction(() => callback(_appPools));
        }

        public static void InstallAdapter()
        {
            using (var sm = new ServerManager())
            {
                var wasConfiguration = sm.GetApplicationHostConfiguration();
                var section = wasConfiguration.GetSection(ListenerAdapterPath);
                var listenerAdaptersCollection = section.GetCollection();
                if (!listenerAdaptersCollection.Any(e => Constants.Scheme.Equals(e.GetAttributeValue("name"))))
                {
                    var element = listenerAdaptersCollection.CreateElement();
                    element.GetAttribute("name").Value = Constants.Scheme;
                    element.GetAttribute("identity").Value = WindowsIdentity.GetCurrent().User.Value;
                    listenerAdaptersCollection.Add(element);
                    sm.CommitChanges();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                EnqueueApplicationAction(apps =>
                {
                    _listenerAdapter.Dispose();
                    foreach (var app in apps.Values)
                    {
                        app.CanOpenNewListenerChannelInstance = false;
                    }
                    _connection.Dispose();
                });
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}