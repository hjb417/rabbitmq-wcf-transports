using System;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel
{
    partial class ConcurrentOperationManager
    {
        private sealed class OperationContext : IDisposable
        {
            private int _decrementCountdown = 1;
            private readonly CountdownEvent _countdownEvent;

            public OperationContext(CountdownEvent countdownEvent, ConcurrentOperationManager manager)
            {
                _countdownEvent = countdownEvent;
                _countdownEvent.AddCount();
                manager.OnOperationContextCreated(EventArgs.Empty);
                if(manager._isDisposed)
                {
                    Dispose();
                    throw new ObjectDisposedException(manager._owningType);
                }
            }

            public void Dispose()
            {
                if (Interlocked.CompareExchange(ref _decrementCountdown, 0, 1) == 1)
                {
                    _countdownEvent.Signal();
                }
            }
        }
    }
}