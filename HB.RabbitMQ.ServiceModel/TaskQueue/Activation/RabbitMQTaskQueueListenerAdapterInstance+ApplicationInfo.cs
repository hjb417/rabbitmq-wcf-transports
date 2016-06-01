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
using HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    partial class RabbitMQTaskQueueListenerAdapterInstance
    {
        private sealed class ApplicationInfo
        {
            private bool _canOpenNewListenerChannelInstance;
            private bool _sysCanOpenNewListenerChannelInstance;
            private ApplicationRequestsBlockedStates _requestsBlockedState;
            private static readonly object _nextIdLock = new object();

            public ApplicationInfo(string applicationKey, string applicationPath, int siteId, string applicationPoolName, ApplicationPoolStates applicationPoolState, ApplicationRequestsBlockedStates requestsBlockedState, Uri messagePublicationNotificationServiceUri, Func<int> channelIdFactory)
            {
                CreationTime = DateTimeOffset.Now;
                _requestsBlockedState = requestsBlockedState;
                ApplicationKey = applicationKey;
                ApplicationPath = applicationPath;
                ApplicationPoolName = applicationPoolName;
                ApplicationPoolState = applicationPoolState;
                ListenerChannelSetup = new ListenerChannelSetup(applicationKey, applicationPath, messagePublicationNotificationServiceUri);
                ListenerChannelId = new Lazy<int>(channelIdFactory);
                SiteId = siteId;
                CanOpenNewListenerChannelInstance = true;
            }

            public string ApplicationKey { get; }
            public string ApplicationPath { get; }
            public int SiteId { get; }
            public DateTimeOffset CreationTime { get; }
            public ListenerChannelSetup ListenerChannelSetup { get; }
            public string ApplicationPoolName { get; private set; }
            public ApplicationPoolStates ApplicationPoolState { get; private set; }
            public Lazy<int> ListenerChannelId { get; }

            public ApplicationRequestsBlockedStates RequestsBlockedState
            {
                get { return _requestsBlockedState; }
                set
                {
                    _requestsBlockedState = value;
                    UpdateCanOpenNewListenerChannelInstance();
                }
            }

            public bool CanOpenNewListenerChannelInstance
            {
                get { return _sysCanOpenNewListenerChannelInstance; }
                set
                {
                    _canOpenNewListenerChannelInstance = value;
                    UpdateCanOpenNewListenerChannelInstance();
                    TraceInformation($"{nameof(CanOpenNewListenerChannelInstance)} is {CanOpenNewListenerChannelInstance} for the application [{ApplicationPoolName}|{ApplicationPath}].", GetType());
                }
            }

            private void UpdateCanOpenNewListenerChannelInstance()
            {
                _sysCanOpenNewListenerChannelInstance =
                    _canOpenNewListenerChannelInstance
                    &&
                    (RequestsBlockedState == ApplicationRequestsBlockedStates.Processsed)
                    &&
                    (ApplicationPoolState == ApplicationPoolStates.Enabled);
            }

            public void UpdateApplicationPool(string applicationPoolName, ApplicationPoolStates applicationPoolState)
            {
                ApplicationPoolName = applicationPoolName;
                ApplicationPoolState = applicationPoolState;
                UpdateCanOpenNewListenerChannelInstance();
            }
        }
    }
}