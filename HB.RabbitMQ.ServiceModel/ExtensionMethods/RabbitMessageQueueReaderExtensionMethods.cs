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
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace HB
{
    internal static class RabbitMessageQueueReaderExtensionMethods
    {
        public static Message Dequeue(this IRabbitMQReader rabbitMessageQueueReader, RabbitMQTaskQueueBinding binding, MessageEncoderFactory messageEncoderFactory, TimeSpan timeout, CancellationToken cancelToken)
        {
            ulong deliveryTag;
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            var msg = rabbitMessageQueueReader.Dequeue(binding, messageEncoderFactory, timeoutTimer.RemainingTime, cancelToken, out deliveryTag);
            rabbitMessageQueueReader.AcknowledgeMessage(deliveryTag, timeoutTimer.RemainingTime, cancelToken);
            return msg;
        }

        public static Message Dequeue(this IRabbitMQReader rabbitMessageQueueReader, RabbitMQTaskQueueBinding binding, MessageEncoderFactory messageEncoderFactory, TimeSpan timeout, CancellationToken cancelToken, out ulong deliveryTag)
        {
            var timeoutTimer = TimeoutTimer.StartNew(timeout);
            var msg = rabbitMessageQueueReader.Dequeue(timeoutTimer.RemainingTime, cancelToken);
            deliveryTag = msg.DeliveryTag;
            Message result;
            try
            {
                result = messageEncoderFactory.Encoder.ReadMessage(msg.Body, (int)binding.MaxReceivedMessageSize);
            }
            catch
            {
                rabbitMessageQueueReader.RejectMessage(msg.DeliveryTag, timeoutTimer.RemainingTime, cancelToken);
                throw;
            }
            return result;
        }
    }
}
