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
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Activation;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.Configuration;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    public sealed class RabbitMQTaskQueueHostedTransportConfiguration : HostedTransportConfiguration
    {
        private static readonly Uri[] _baseAddresses = new Uri[0];
        
        static RabbitMQTaskQueueHostedTransportConfiguration()
        {
            var config = (RabbitMQTaskQueueWasConfigurationSection)ConfigurationManager.GetSection("rabbitMQTaskQueueWasConfiguration");
            if(config != null)
            {
                _baseAddresses = config.BaseAddresses
                    .Select(b => new UriBuilder(Constants.Scheme, b.Hostname, b.Port).Uri)
                    .ToArray();
            }
        }

        public RabbitMQTaskQueueHostedTransportConfiguration()
        {
        }

        public override Uri[] GetBaseAddresses(string virtualPath)
        {
            Debug.WriteLine($"{nameof(RabbitMQTaskQueueHostedTransportConfiguration)}.{nameof(GetBaseAddresses)}({virtualPath})");
            var uris = _baseAddresses
                .Select(b => new Uri(b, virtualPath))
                .ToArray();
            return uris;
        }
    }
}