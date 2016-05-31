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
using System.ServiceModel;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;
using static HB.RabbitMQ.ServiceModel.Diagnostics.TraceHelper;

namespace HB.RabbitMQ.ServiceModel.TaskQueue.Activation
{
    partial class MessagePublicationNotificationService
    {
        private sealed class Client : IDisposable
        {
            private readonly ConcurrentDictionary<string, string> _activatedServices = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly ConcurrentDictionary<string, string> _missingServices = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private readonly IWasInteropServiceCallback _callback;
            private readonly MessagePublicationNotificationService _msgPubSvc;

            public Client(MessagePublicationNotificationService msgPubService, IWasInteropServiceCallback callback, string applicationPath, int listenerChannelId, Guid id)
            {
                CreationTime = DateTimeOffset.Now;
                _msgPubSvc = msgPubService;
                _callback = callback;
                ApplicationPath = applicationPath;
                ListenerChannelId = listenerChannelId;
                Id = id;
            }

            public int ListenerChannelId { get; }
            public Guid Id { get; }
            public DateTimeOffset CreationTime { get; }
            public string ApplicationPath { get; }

            public void AddActivatedService(string servicePath)
            {
                if (_activatedServices.TryAdd(servicePath, null))
                {
                    TraceInformation($"Activated service [{servicePath}].", GetType());
                }
            }

            public void EnsureServiceAvailable(string servicePath)
            {
                if(_activatedServices.ContainsKey(servicePath))
                {
                    return;
                }
                if (_missingServices.ContainsKey(servicePath))
                {
                    return;
                }
                TraceInformation($"Activating service [{servicePath}].", GetType());
                try
                {
                    _callback.EnsureServiceAvailable(servicePath);
                    AddActivatedService(servicePath);
                }
                catch (Exception e) when ((e is ObjectDisposedException) || (e is CommunicationException))
                {
                    TraceWarning($"Failed to ensure service is available for the listener channel [{Id}-{ListenerChannelId}|{ApplicationPath}] created on {CreationTime}. {e}", GetType());
                    _msgPubSvc.Unregister(Id, e.Message);
                }
            }

            public void AddMissingService(string servicePath)
            {
                if (_missingServices.TryAdd(servicePath, null))
                {
                    TraceInformation($"Added missing service [{servicePath}].", GetType());
                }
            }

            public void KeepAlive()
            {
                try
                {
                    _callback.KeepAlive();
                }
                catch (Exception e) when ((e is ObjectDisposedException) || (e is CommunicationException))
                {
                    TraceWarning($"Failed to perform keep-alive for the client [{Id}-{ListenerChannelId}|{ApplicationPath}] that registered on {CreationTime}. {e}", GetType());
                    _msgPubSvc.Unregister(Id, e.Message);
                }
            }

            public void Dispose()
            {
                ((ICommunicationObject)_callback).Dispose();
            }
        }
    }
}