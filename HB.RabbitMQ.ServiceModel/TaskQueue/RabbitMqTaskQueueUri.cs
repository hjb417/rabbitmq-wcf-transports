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
