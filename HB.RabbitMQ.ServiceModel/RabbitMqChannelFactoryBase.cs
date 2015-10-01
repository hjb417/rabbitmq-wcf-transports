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
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel
{
    internal abstract class RabbitMQChannelFactoryBase<TChannel> : ChannelFactoryBase<TChannel>
    {
        protected RabbitMQChannelFactoryBase(IDefaultCommunicationTimeouts timeouts)
            : base(timeouts)
        {
        }

        protected override sealed void OnAbort()
        {
            MethodInvocationTrace.Write();
            OnClose(TimeSpan.Zero, CloseReasons.Abort);
            base.OnAbort();
        }

        protected override sealed void OnClose(TimeSpan timeout)
        {
            MethodInvocationTrace.Write();
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            OnClose(timeoutTimer.RemainingTime, CloseReasons.StateTransition);
            base.OnClose(timeoutTimer.RemainingTime);
        }

        protected virtual void OnClose(TimeSpan timeout, CloseReasons closeReason)
        {
        }
    }
}
