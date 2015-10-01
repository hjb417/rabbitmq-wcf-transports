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
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace HB
{
    internal static class RabbitMessageQueueWriterExtensionMethods
    {
        public static void Enqueue(this IRabbitMQWriter rabbitMessageQueueWriter, string exchange, string queueName, Message message, BufferManager bufferManager, RabbitMQTaskQueueBinding binding, MessageEncoderFactory messageEncoderFactory, TimeSpan timeToLive, TimeSpan timeout, CancellationToken cancelToken)
        {
            var buffer = messageEncoderFactory.Encoder.WriteMessage(message, (int)binding.MaxReceivedMessageSize, bufferManager);
            try
            {
                using (var messageStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count))
                {
                    rabbitMessageQueueWriter.Enqueue(exchange, queueName, messageStream, timeToLive, timeout, cancelToken);
                }
            }
            finally
            {
                bufferManager.ReturnBuffer(buffer.Array);
            }
        }
    }
}