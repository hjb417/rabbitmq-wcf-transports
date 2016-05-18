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
    internal class RabbitMQQueueMonitor : IDisposable
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
            _refreshTimer = new Timer(state =>
            {
                var lockTaken = false;
                try
                {
                    Monitor.TryEnter(refreshLock, ref lockTaken);
                    if (lockTaken)
                    {
                        Refresh();
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(refreshLock);
                    }
                }
            }, null, TimeSpan.Zero, pollInterval);
        }

        ~RabbitMQQueueMonitor()
        {
            Dispose(false);
        }

        public IEnumerable<string> QueueMessageCount { get { return _queueStats.Keys; } }

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
            using (var client = CreateWebClient())
            {
                var json = client.DownloadString("/api/queues");
                var queues = _deserializer.Deserialize<Dictionary<string, object>[]>(json)
                    .Where(q => q.ContainsKey("message_stats"))
                    .Select(q => new
                    {
                        QueueName = (string)q["name"],
                        MessageStats = (Dictionary<string, object>)q["message_stats"],
                    })
                    .Where(q => q.MessageStats.ContainsKey("messages_ready"))
                    .Select(q => new
                    {
                        q.QueueName,
                        PublishCount = Convert.ToInt64(q.MessageStats["publish"]),
                        MessagesReady = Convert.ToInt64(q.MessageStats["messages_ready"]),
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
                    var stats = _queueStats.GetOrAdd(queue.QueueName);
                    stats.MessagesReady = queue.MessagesReady;

                    if (stats.PublishCount != queue.PublishCount)
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