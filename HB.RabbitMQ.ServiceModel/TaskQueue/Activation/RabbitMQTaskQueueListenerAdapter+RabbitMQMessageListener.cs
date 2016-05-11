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
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    partial class RabbitMQTaskQueueListenerAdapter
    {
        private sealed class RabbitMQMessageListener
        {
            private readonly IConnection _connection;
            private IModel _channel = null;

            public event EventHandler MessageQueuedEvent;

            public RabbitMQMessageListener(ApplicationInfo application, IConnection connection)
            {
                _connection = connection;
                EventHandler callback = delegate
                {
                    if (application.CanOpenNewListenerChannelInstance)
                    {
                        SubscribeForMessagePublicationNotification(application.ApplicationPath);
                    }
                    else
                    {
                        UnsubscribeFromMessagePublicationNotification();
                    }
                };
                application.CanOpenNewListenerChannelInstanceChanged += callback;
                callback(application, EventArgs.Empty);
            }

            private void SubscribeForMessagePublicationNotification(string applicationPath)
            {
                _channel = _connection.CreateModel();
                var queue = _channel.QueueDeclare();
                _channel.QueueBind(queue, PredeclaredExchangeNames.Topic, applicationPath);
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (s, e) => MessageQueuedEvent?.Invoke(this, EventArgs.Empty);
                _channel.BasicConsume(queue, true, consumer);
            }

            private void UnsubscribeFromMessagePublicationNotification()
            {
                _channel?.Dispose();
            }
        }
    }
}
