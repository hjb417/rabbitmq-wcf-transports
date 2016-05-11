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
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using HB.RabbitMQ.ServiceModel.Activation.Microsoft.Win32.SafeHandles;

namespace HB.RabbitMQ.ServiceModel.Activation.Runtime.InteropServices
{

    internal static class NativeMethods
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFreeLibrary LoadLibraryEx([In] string lpFileName, [In] IntPtr hFile, [In] LoadLibraryFlags dwFlags);

        [DllImport("kernel32", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr GetProcAddress([In] SafeFreeLibrary hModule, [In] string lpProcName);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool _SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);

        [DllImport("advapi32", SetLastError = true)]
        private static extern bool _QueryServiceStatus(IntPtr handle, out ServiceStatus serviceStatus);

        public static void SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus)
        {
            if(!_SetServiceStatus(handle, ref serviceStatus))
            {
                throw new Win32Exception();
            }
        }

        public static ServiceStatus QueryServiceStatus(IntPtr handle)
        {
            ServiceStatus status;
            if (!_QueryServiceStatus(handle, out status))
            {
                throw new Win32Exception();
            }
            return status;
        }
    }
}
