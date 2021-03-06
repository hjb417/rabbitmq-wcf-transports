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
using RabbitMQ.Client;

namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    partial class RabbitMQTaskQueueBindingElement
    {
        internal sealed class DefaultValues
        {
            public const string HostName = "localhost";
            public const int Port = AmqpTcpEndpoint.UseDefaultPort;
            public const long MaxBufferPoolSize = 524288;
            public const long MaxReceivedMessageSize = 65536;
            public const string TaskQueueTimeToLive = null;
            public const string ReplyQueueTimeToLive = "00:20:00";
            public const string Password = ConnectionFactory.DefaultPass;
            public const string Username = ConnectionFactory.DefaultUser;
            public const string VirtualHost = ConnectionFactory.DefaultVHost;
            public const string Protocol = AmqpProtocols.Default;
            public const bool IncludeProcessCommandLineInQueueArguments = false;
            public const bool IncludeProcessCommandLineInMessageHeaders = false;
            public const string WriterOptions = null;
            public const string ReaderOptions = null;
            public const string MaxPriority = null;
            public const string Exchange = Constants.DefaultExchange;
            public const string DeleteOnClose = "false";
            public const string IsDurable = "true";
            public const string AutomaticRecoveryEnabled = "true";
            public const string RequestedHeartbeat = "00:03";
            public const string UseBackgroundThreadsForIO = "true";
            public const string TransactionFlow = "true";
        }
    }
}