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
using System.Collections.Specialized;
using System.Web;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    [Serializable]
    public partial class RabbitMQTaskQueueUri : Uri
    {
        public RabbitMQTaskQueueUri(string uri)
            : base(uri)
        {
            var qs = HttpUtility.ParseQueryString(Query);
            Exchange = qs[QueryKeys.Exchange] ?? Constants.DefaultExchange;
            IsDurable = qs.ContainsKey(QueryKeys.Durable) ? bool.Parse(qs[QueryKeys.Durable]) : true;
            DeleteOnClose = qs.ContainsKey(QueryKeys.DeleteOnClose) ? bool.Parse(qs[QueryKeys.DeleteOnClose]) : false;
            TimeToLive = qs.ContainsKey(QueryKeys.Ttl) ? TimeSpan.Parse(qs[QueryKeys.Ttl]) : default(TimeSpan?);
        }

        public static RabbitMQTaskQueueUri Create(string queueName, string exhangeName = Constants.DefaultExchange, bool durable = true, bool deleteOnClose = false, TimeSpan? ttl = null)
        {
            var queryStringValues = new NameValueCollection(StringComparer.OrdinalIgnoreCase);
            queryStringValues[QueryKeys.Exchange] = exhangeName;
            queryStringValues[QueryKeys.Durable] = durable.ToString();
            queryStringValues[QueryKeys.DeleteOnClose] = deleteOnClose.ToString();
            if (ttl.HasValue)
            {
                queryStringValues[QueryKeys.Ttl] = ttl.ToString();
            }

            return new RabbitMQTaskQueueUri(string.Format("{0}://{1}?{2}", Constants.Scheme, queueName, queryStringValues.ToQueryString()));
        }

        public string QueueName { get { return Host; } }
        public string Exchange { get; private set; }
        public bool IsDurable { get; private set; }
        public bool DeleteOnClose { get; private set; }
        public TimeSpan? TimeToLive { get; private set; }
    }
}
