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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web.Script.Serialization;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    internal class RabbitMQQueueMonitor : IRabbitMQQueueMonitor
    {
        private readonly Timer _refreshTimer;
        private readonly JavaScriptSerializer _deserializer = new JavaScriptSerializer();
        private readonly ConcurrentDictionary<string, QueueStatistics> _queueStats = new ConcurrentDictionary<string, QueueStatistics>();
        private readonly Uri _rabbitMqManagementUri;

        public event EventHandler<MessagePublishedEventArgs> MessagePublished;
        public event EventHandler<QueueDeletedEventArgs> QueueDeleted;

        public RabbitMQQueueMonitor(Uri rabbitMqManagementUri, TimeSpan pollInterval)
        {
            _rabbitMqManagementUri = rabbitMqManagementUri;
            var refreshLock = new object();
            _refreshTimer = new Timer(RefreshTimerCallback, refreshLock, pollInterval, pollInterval);
            RefreshTimerCallback(refreshLock);
        }

        ~RabbitMQQueueMonitor()
        {
            Dispose(false);
        }

        public IEnumerable<string> QueueMessageCount { get { return _queueStats.Keys; } }

        private void RefreshTimerCallback(object refreshLock)
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(refreshLock, ref lockTaken);
                if (lockTaken)
                {
                    try
                    {
                        Refresh();
                    }
                    catch(Exception e)
                    {
                        TraceError(e.ToString(), GetType());
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(refreshLock);
                }
            }
        }

        private WebClient CreateWebClient()
        {
            var client = new WebClient { BaseAddress = _rabbitMqManagementUri.ToString() };
            if (_rabbitMqManagementUri.UserInfo != string.Empty)
            {
                var credentials = _rabbitMqManagementUri.UserInfo.Split(':');
                var userName = Uri.UnescapeDataString(credentials[0]);
                var password = Uri.UnescapeDataString(credentials[1]);
                client.Credentials = new NetworkCredential(userName, password);
            }
            return client;
        }

        private void Refresh()
        {
            TraceInformation("Querying RabbitMQ Management service.", GetType());
            using (var client = CreateWebClient())
            {
                var json = client.DownloadString("/api/queues");
                var queues = _deserializer.Deserialize<Dictionary<string, object>[]>(json)
                    .Where(q => q.ContainsKey("message_stats"))
                    .Where(q => q.ContainsKey("messages_ready"))
                    .Where(q => q.ContainsKey("arguments"))
                    .Select(q => new
                    {
                        Arguments = (Dictionary<string, object>) q["arguments"],
                        QueueName = (string)q["name"],
                        MessagesReady = Convert.ToInt64(q["messages_ready"]),
                        MessageStats = (Dictionary<string, object>)q["message_stats"],
                    })
                    .Where(q => Constants.Scheme.Equals(q.Arguments.GetValueOrDefault(TaskQueueReaderQueueArguments.Scheme)))
                    .Where(q => true.Equals(q.Arguments.ContainsKey(TaskQueueReaderQueueArguments.IsTaskInputQueue)))
                    .Select(q => new
                    {
                        q.QueueName,
                        PublishCount = Convert.ToInt64(q.MessageStats.GetValueOrDefault("publish") ?? 0),
                        q.MessagesReady,
                    })
                    .ToArray();
                var queuesToRemove = _queueStats.Keys.Except(queues.Select(q => q.QueueName)).ToArray();
                foreach (var queueToRemove in queuesToRemove)
                {
                    _queueStats.Remove(queueToRemove);
                    OnQueueDeleted(new QueueDeletedEventArgs(queueToRemove));
                }
                foreach (var queue in queues)
                {
                    var stats = _queueStats.GetValueOrDefault(queue.QueueName);
                    bool publish = false;
                    if (stats == null)
                    {
                        stats = new QueueStatistics();
                        _queueStats.Add(queue.QueueName, stats);
                        publish = (queue.PublishCount > 0);
                    }
                    stats.MessagesReady = queue.MessagesReady;
                    if (stats.PublishCount != queue.PublishCount)
                    {
                        publish = true;
                    }
                    if (publish)
                    {
                        stats.UpdatePublishInfo(queue.PublishCount);
                        var args = new MessagePublishedEventArgs(queue.QueueName, GetApplicationPath(queue.QueueName));
                        OnMessagePublished(args);
                    }
                }
            }
        }

        public IEnumerable<string> GetQueuesWithPendingMessages(string applicationPath)
        {
            return _queueStats
                .Where(s => s.Value.MessagesReady > 0)
                .Select(s => new
                {
                    ApplicationPath = GetApplicationPath(s.Key),
                    QueueName = s.Key,
                })
                .Where(q => applicationPath.Equals(q.ApplicationPath, StringComparison.OrdinalIgnoreCase))
                .Select(q => q.QueueName);
        }

        private string GetApplicationPath(string queueName)
        {
            return Path.GetDirectoryName(queueName).Replace(Path.DirectorySeparatorChar, '/');
        }

        public bool ApplicationHasPendingMessages(string applicationPath)
        {
            return _queueStats
                .Where(s => s.Value.MessagesReady > 0)
                .Select(s => GetApplicationPath(s.Key))
                .Contains(applicationPath, StringComparer.OrdinalIgnoreCase);
        }

        protected virtual void OnMessagePublished(MessagePublishedEventArgs e)
        {
            TraceInformation($"OnMessagePublished[{nameof(e.ApplicationPath)}={e.ApplicationPath}, {nameof(e.QueueName)}={e.QueueName}].", GetType());
            MessagePublished?.Invoke(this, e);
        }

        protected virtual void OnQueueDeleted(QueueDeletedEventArgs e)
        {
            QueueDeleted?.Invoke(this, e);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}