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
using System.Threading;
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel
{
    public sealed class RabbitMQReaderSetup : ICloneable
    {
        public IConnectionFactory ConnectionFactory { get; set; }
        public string Exchange { get; set; }
        public string QueueName { get; set; }
        public bool IsDurable { get; set; }
        public bool DeleteQueueOnClose { get; set; }
        public TimeSpan? QueueTimeToLive { get; set; }
        public TimeSpan Timeout { get; set; }
        public CancellationToken CancelToken { get; set; }
        public RabbitMQReaderOptions Options { get; set; }
        public int? MaxPriority { get; set; }
        public Dictionary<string, object> QueueArguments { get; set; }

        public RabbitMQReaderSetup Clone()
        {
            var clone = (RabbitMQReaderSetup)MemberwiseClone();
            clone.Options = clone.Options?.Clone();
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }
}