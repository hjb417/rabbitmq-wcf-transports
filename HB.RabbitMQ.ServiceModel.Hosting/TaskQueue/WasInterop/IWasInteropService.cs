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
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop
{
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(IWasInteropServiceCallback))]
    public interface IWasInteropService
    {
        [OperationContract(IsInitiating = true)]
        void Register(int listenerChannelId, Guid appDomainProcotolHandlerId, string applicationPath);

        [OperationContract(IsOneWay = true, IsTerminating = true, IsInitiating = false)]
        void Unregister(Guid appDomainProcotolHandlerId, string reason);

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void ServiceActivated(Guid appDomainProcotolHandlerId, string virtualPath);

        [OperationContract(IsOneWay = true, IsInitiating = false)]
        void ServiceNotFound(Guid appDomainProcotolHandlerId, string virtualPath);
    }
}