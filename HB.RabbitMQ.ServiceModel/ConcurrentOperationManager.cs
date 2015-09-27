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