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
using System.Runtime.InteropServices;

namespace HB.RabbitMQ.ServiceModel.Activation.Runtime.InteropServices
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct WebhostListenerCallbacks
    {
        public int BytesInCallbackStructure;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ConfigManagerConnected ConfigManagerConnected;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ConfigManagerDisconnected ConfigManagerDisconnected;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ConfigManagerInitializationCompleted ConfigManagerInitializationCompleted;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolCreated ApplicationPoolCreated;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolDeleted ApplicationPoolDeleted;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolIdentityChanged ApplicationPoolIdentityChanged;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolStateChanged ApplicationPoolStateChanged;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolCanOpenNewListenerChannelInstance ApplicationPoolCanOpenNewListenerChannelInstance;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationPoolAllListenerChannelInstancesStopped ApplicationPoolAllListenerChannelInstancesStopped;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationCreated ApplicationCreated;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationDeleted ApplicationDeleted;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationBindingsChanged ApplicationBindingsChanged;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationAppPoolChanged ApplicationAppPoolChanged;

        [MarshalAs(UnmanagedType.FunctionPtr)]
        public ApplicationRequestsBlockedChanged ApplicationRequestsBlockedChanged;
    }
}
