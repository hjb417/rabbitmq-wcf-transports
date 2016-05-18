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
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter;
using HB.RabbitMQ.ServiceModel.Hosting.ServiceModel;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;
using HB.RabbitMQ.ServiceModel.Threading.Tasks.Schedulers;
using Microsoft.Web.Administration;
using ListenerAdapter = HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter.ListenerAdapter;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    public partial class RabbitMQTaskQueueListenerAdapter : IDisposable
    {
        private const string ListenerAdapterPath = "system.applicationHost/listenerAdapters";
        private const string ProtocolsPath = "system.web/protocols";

        private readonly Dictionary<string, ApplicationInfo> _apps = new Dictionary<string, ApplicationInfo>();
        private readonly Dictionary<string, ApplicationPoolInfo> _appPools = new Dictionary<string, ApplicationPoolInfo>();
        private readonly ListenerAdapter _listenerAdapter;
        private readonly RabbitMQQueueMonitor _queueMon;
        private readonly LimitedConcurrencyLevelTaskScheduler _taskScheduler = new LimitedConcurrencyLevelTaskScheduler(1);
        private volatile bool _isDisposed;
        private Uri _messagePublicationNotificationServiceUri = new Uri($"net.pipe://localhost/RabbitMQTaskQueueListenerAdapter_{Guid.NewGuid():N}");
        private readonly ServiceHost _msgPubNotificationSvcHost;

        public RabbitMQTaskQueueListenerAdapter(Uri rabbitMqManagementUri, TimeSpan pollInterval)
        {
            _queueMon = new RabbitMQQueueMonitor(rabbitMqManagementUri, pollInterval);
            _queueMon.MessagePublished += QueueMonitor_MessagePublished;

            _msgPubNotificationSvcHost = new ServiceHost(new MessagePublicationNotificationService(_queueMon), _messagePublicationNotificationServiceUri);
            _msgPubNotificationSvcHost.AddServiceEndpoint(typeof(IWasInteropService), NetNamedPipeBindingFactory.Create(), string.Empty);
            _msgPubNotificationSvcHost.Open();

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

        private void QueueMonitor_MessagePublished(object sender, MessagePublishedEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var appsToOpen = apps.Values
                    .Where(a => a.ListenerChannelId.IsValueCreated)
                    .Where(a => a.ApplicationPath.Equals(e.ApplicationPath, StringComparison.OrdinalIgnoreCase));
                foreach (var app in appsToOpen)
                {
                    OpenNewListenerChannelInstance(app);
                }
            });
        }

        private void ListenerAdapter_ApplicationPoolDeleted(object sender, ApplicationPoolDeletedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools =>
            {
                TraceInformation($"Deleting application pool [{e.ApplicationPoolId}].", GetType());
                appPools.Remove(e.ApplicationPoolId);
            });
        }

        private void ListenerAdapter_ApplicationPoolCreated(object sender, ApplicationPoolCreatedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools =>
            {
                TraceInformation($"Adding the application pool [{e.ApplicationPoolName}].", GetType());
                appPools[e.ApplicationPoolName] = new ApplicationPoolInfo(e.ApplicationPoolName);
            });
        }

        private void ListenerAdapter_ApplicationAppPoolChanged(object sender, ApplicationAppPoolChangedEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = apps[e.ApplicationName];
                var appPoolState = _appPools[e.ApplicationPoolName].State;
                TraceInformation($"Changing the application pool to [{e.ApplicationPoolName}:{appPoolState}] for the application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
                app.UpdateApplicationPool(e.ApplicationPoolName, appPoolState);
            });
        }

        private void ListenerAdapter_ApplicationPoolStateChanged(object sender, ApplicationPoolStateChangedEventArgs e)
        {
            EnqueueApplcationPoolAction(appPools => appPools[e.ApplicationPoolId].State = e.State);
            EnqueueApplicationAction(apps =>
            {
                foreach (var app in apps.Values)
                {
                    if (app.ApplicationPoolName == e.ApplicationPoolId)
                    {
                        TraceInformation($"Changing {nameof(app.ApplicationPoolState)} to [{e.State}] for the application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
                        app.UpdateApplicationPool(e.ApplicationPoolId, e.State);
                    }
                }
            });
        }

        private void ListenerAdapter_ApplicationRequestBlockedStateChanged(object sender, ApplicationRequestBlockedStateChangedEventArgs e)
        {
            EnqueueApplicationAction(apps =>
            {
                var app = apps[e.ApplicationName];
                TraceInformation($"Changing {nameof(app.RequestsBlockedState)} to [{e.State}] for the application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
                app.RequestsBlockedState = e.State;
            });
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
                    TraceInformation($"Stopping application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
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
                    TraceInformation($"Removing application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
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
                var app = new ApplicationInfo(e.ApplicationKey, e.ApplicationVirtualPath, e.SiteId, e.ApplicationPoolName, _appPools[e.ApplicationPoolName].State, e.RequestsBlockedState, _messagePublicationNotificationServiceUri);
                apps.Add(e.ApplicationVirtualPath, app);
                TraceInformation($"Created application [{app.ApplicationKey}:{app.ApplicationPath}].", GetType());
                if (_queueMon.ApplicationHasPendingMessages(app.ApplicationPath))
                {
                    OpenNewListenerChannelInstance(app);
                }
            });
        }

        private void OpenNewListenerChannelInstance(ApplicationInfo app)
        {
            if (app.ListenerChannelId.IsValueCreated)
            {
                return;
            }
            if (!app.CanOpenNewListenerChannelInstance)
            {
                return;
            }
            if(_isDisposed)
            {
                return;
            }
            app.CanOpenNewListenerChannelInstance = false;
            TraceInformation($"Opening listener channel for application [{app.ApplicationPoolName}:{app.ApplicationPath}] with listener channel id [{app.ListenerChannelId.Value}].", GetType());
            _listenerAdapter.OpenListenerChannelInstance(app.ApplicationPoolName, app.ListenerChannelId.Value, app.ListenerChannelSetup.ToBytes());
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
                if (listenerAdaptersCollection.Any(e => Constants.Scheme.Equals(e.GetAttributeValue("name"))))
                {
                    return;
                }
                var element = listenerAdaptersCollection.CreateElement();
                element.GetAttribute("name").Value = Constants.Scheme;
                element.GetAttribute("identity").Value = WindowsIdentity.GetCurrent().User.Value;
                listenerAdaptersCollection.Add(element);
                sm.CommitChanges();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                EnqueueApplicationAction(apps =>
                {
                    _queueMon.Dispose();
                    _listenerAdapter.Dispose();
                    foreach (var app in apps.Values)
                    {
                        app.CanOpenNewListenerChannelInstance = false;
                    }
                    try
                    {
                        _msgPubNotificationSvcHost.Close();
                    }
                    catch
                    {
                        _msgPubNotificationSvcHost.Abort();
                    }
                    var msgPubNotificationSvc = (MessagePublicationNotificationService)_msgPubNotificationSvcHost.SingletonInstance;
                    msgPubNotificationSvc.Dispose();
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