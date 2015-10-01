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
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel
{
    internal sealed partial class ConcurrentOperationManager : IDisposable
    {
        private readonly CountdownEvent _usageCountdown = new CountdownEvent(1);
        private volatile bool _isDisposed;
        private int _waitForCountdown = 1;
        private readonly string _owningType;
        private readonly CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();

        internal event EventHandler OperationContextCreated;

        public ConcurrentOperationManager(string owningType)
        {
            _owningType = owningType;
        }

        [ExcludeFromCodeCoverage]
        ~ConcurrentOperationManager()
        {
            Dispose(false);
        }

        internal bool IsDisposed { get { return _isDisposed; } }

        public CancellationToken Token
        {
            get { return _cancelTokenSource.Token; }
        }

        public IDisposable TrackOperation()
        {
            if(_isDisposed)
            {
                throw new ObjectDisposedException(_owningType);
            }
            return new OperationContext(_usageCountdown, this);
        }

        private void OnOperationContextCreated(EventArgs e)
        {
            var subscribers = OperationContextCreated;
            if(subscribers != null)
            {
                subscribers(this, e);
            }
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                if (Interlocked.CompareExchange(ref _waitForCountdown, 0, 1) == 1)
                {
                    _cancelTokenSource.Cancel();
                    _usageCountdown.Signal();
                    _usageCountdown.Wait();
                }
                _cancelTokenSource.Dispose();
                _usageCountdown.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}