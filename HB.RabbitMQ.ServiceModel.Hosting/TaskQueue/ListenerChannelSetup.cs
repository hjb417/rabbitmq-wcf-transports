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
using System.Runtime.Serialization;

namespace HB.RabbitMQ.ServiceModel.Hosting.TaskQueue
{
    [DataContract(IsReference = true)]
    public sealed class ListenerChannelSetup
    {
        public ListenerChannelSetup(string applicationId, string applicationPath, Uri messagePublicationNotificationServiceUri)
        {
            ApplicationId = applicationId;
            ApplicationPath = applicationPath;
            MessagePublicationNotificationServiceUri = messagePublicationNotificationServiceUri;
        }

        [DataMember]
        public string ApplicationId { get; private set; }

        [DataMember]
        public string ApplicationPath { get; private set; }

        [DataMember]
        public Uri MessagePublicationNotificationServiceUri { get; private set; }

        public static ListenerChannelSetup FromBytes(byte[] buffer)
        {
            var serializer = new DataContractSerializer(typeof(ListenerChannelSetup));
            using (var input = new MemoryStream(buffer, false))
            {
                return (ListenerChannelSetup)serializer.ReadObject(input);
            }
        }

        public byte[] ToBytes()
        {
            var serializer = new DataContractSerializer(typeof(ListenerChannelSetup));
            using (var buffer = new MemoryStream())
            {
                serializer.WriteObject(buffer, this);
                return buffer.ToArray();
            }
        }
    }
}