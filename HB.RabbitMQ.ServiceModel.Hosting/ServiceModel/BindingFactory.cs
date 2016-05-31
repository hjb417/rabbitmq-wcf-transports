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
using System.ServiceModel.Channels;

namespace HB.RabbitMQ.ServiceModel.Hosting.ServiceModel
{
    public static partial class BindingFactory
    {
        public static Binding Create(Uri uri)
        {
            if(uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }
            if(Uri.UriSchemeHttp.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return BasicHttpBindingFactory.Create();
            }
            if(Uri.UriSchemeNetPipe.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return NetNamedPipeBindingFactory.Create();
            }
            if (Uri.UriSchemeNetTcp.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return NetTcpBindingFactory.Create();
            }
            throw new NotSupportedException($"The scheme {uri.Scheme} is not supported.");
        }
    }
}