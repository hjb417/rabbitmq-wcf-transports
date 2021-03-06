﻿/*
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
using System.Diagnostics;
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop
{
    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false, MaxItemsInObjectGraph = int.MaxValue)]
    internal sealed class WasInteropServiceCallback : IWasInteropServiceCallback
    {
        public WasInteropServiceCallback(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
        public IWasInteropService Service { get; set; }

        public void EnsureServiceAvailable(string virtualPath)
        {
            Trace.TraceInformation($"{nameof(WasInteropServiceCallback)}.{nameof(EnsureServiceAvailable)}: Activating service [{virtualPath}] for listener channel id [{Id}].");
            try
            {
                ServiceHostingEnvironment.EnsureServiceAvailable(virtualPath);
            }
            catch (EndpointNotFoundException)
            {
                try
                {
                    Service.ServiceNotFound(Id, virtualPath);
                }
                catch (InvalidOperationException e) when (((ICommunicationObject)Service).State != CommunicationState.Opened)
                {
                    Trace.TraceWarning($"{nameof(WasInteropServiceCallback)}.{nameof(EnsureServiceAvailable)}: {e}.");
                }
                return;
            }
            Trace.TraceInformation($"{nameof(WasInteropServiceCallback)}.{nameof(EnsureServiceAvailable)}: Activated service [{virtualPath}] for listener channel id [{Id}].");
            Service.ServiceActivated(Id, virtualPath);
        }

        public void KeepAlive()
        {
        }
    }
}