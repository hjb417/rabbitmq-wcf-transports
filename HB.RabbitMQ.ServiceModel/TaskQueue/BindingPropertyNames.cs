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
namespace HB.RabbitMQ.ServiceModel.TaskQueue
{
    internal static class BindingPropertyNames
    {
        public const string HostName = "hostName";
        public const string Port = "port";
        public const string Password = "password";
        public const string UserName = "username";
        public const string VirtualHost = "virtualHost";
        public const string Protocol = "protocol";
        public const string MaxBufferPoolSize = "maxBufferPoolSize";
        public const string MaxReceivedMessageSize = "maxReceivedMessageSize";
        public const string QueueTimeToLive = "queueTimeToLive";
        public const string WriterOptions = "writerOptions";
        public const string ReaderOptions = "readerOptions";
        public const string IncludeProcessCommandLineInMessageHeaders = "includeProcessCommandLineInMessageHeaders";
        public const string AutoCreateServerQueue = "autoCreateServerQueue";
        public const string MessageConfirmationMode = "messageConfirmationMode";
        public const string Exchange = "exchange";
        public const string IsDurable = "isDurable";
        public const string DeleteOnClose = "deleteOnClose";
        public const string TimeToLive = "timeToLive";
        public const string MaxPriority = "maxPriority";
        public const string AutomaticRecoveryEnabled = "automaticRecoveryEnabled";
        public const string RequestedHeartbeat = "requestedHeartbeat";
        public const string UseBackgroundThreadsForIO = "useBackgroundThreadsForIO";
    }
}