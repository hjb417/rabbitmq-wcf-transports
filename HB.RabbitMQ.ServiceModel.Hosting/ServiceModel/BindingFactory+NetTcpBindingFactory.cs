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
using System.ServiceModel;

namespace HB.RabbitMQ.ServiceModel.Hosting.ServiceModel
{
    partial class BindingFactory
    {
        private static class NetTcpBindingFactory
        {
            public static NetTcpBinding Create()
            {
                var binding = new NetTcpBinding(SecurityMode.None, true);
                binding.CloseTimeout = TimeSpan.FromSeconds(5);
                binding.OpenTimeout = TimeSpan.FromSeconds(5);
                binding.ReceiveTimeout = TimeSpan.MaxValue;
                binding.SendTimeout = TimeSpan.MaxValue;

                binding.ReliableSession.InactivityTimeout = TimeSpan.MaxValue;
                binding.ReliableSession.Ordered = false;
                binding.MaxReceivedMessageSize = int.MaxValue;
                binding.MaxBufferPoolSize = 0;
                binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.None;
                binding.Security.Message.ClientCredentialType = MessageCredentialType.None;

                binding.ReaderQuotas.MaxDepth = int.MaxValue;
                binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
                binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
                binding.ReaderQuotas.MaxNameTableCharCount = int.MaxValue;
                binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
                return binding;
            }
        }
    }
}