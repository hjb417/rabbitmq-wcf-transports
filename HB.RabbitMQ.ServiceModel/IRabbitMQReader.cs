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
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HB.RabbitMQ.ServiceModel
{
    internal interface IRabbitMQReader : IDisposable
    {
        string Exchange { get; }
        string QueueName { get; }
        void AcknowledgeMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken);
        void RejectMessage(ulong deliveryTag, TimeSpan timeout, CancellationToken cancelToken);
        QueueDeclareOk QueryQueue(TimeSpan timeout, CancellationToken cancelToken);
        uint MessageCount(TimeSpan timeout, CancellationToken cancelToken);
        DequeueResult Dequeue(TimeSpan timeout, CancellationToken cancelToken);
        bool WaitForMessage(TimeSpan timeout, CancellationToken cancelToken);
        EventingBasicConsumer CreateEventingBasicConsumer(string routingKey, string exchange, TimeSpan timeout, CancellationToken cancelToken);
        void SoftClose();
    }
}