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
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace HB.RabbitMQ.ServiceModel.Activation.ListenerAdapter
{
    [Serializable]
    public class ApplicationCreatedEventArgs : EventArgs
    {
        public ApplicationCreatedEventArgs(string applicationKey, string url, int siteId, string applicationPoolId, IEnumerable<string> bindings, ApplicationRequestsBlockedStates requestsBlockedState)
        {
            ApplicationKey = applicationKey;
            Url = url;
            SiteId = siteId;
            ApplicationPoolName = applicationPoolId;
            Bindings = bindings;
            RequestsBlockedState = requestsBlockedState;
        }

        public string ApplicationKey { get; }
        public string ApplicationPoolName { get; }
        public IEnumerable<string> Bindings { get; }
        public ApplicationRequestsBlockedStates RequestsBlockedState { get; }
        public int SiteId { get; }
        public string Url { get; }
    }
}
