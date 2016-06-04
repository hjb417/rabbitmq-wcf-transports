using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public abstract class UnitTest : IDisposable
    {
        private readonly TestOutputHelperTraceListener _xunitTraceListener;

        protected UnitTest(ITestOutputHelper outputHelper)
        {
            _xunitTraceListener = new TestOutputHelperTraceListener(outputHelper);
            Trace.Listeners.Add(_xunitTraceListener);
        }

        ~UnitTest()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                Trace.Flush();
                if (_xunitTraceListener != null)
                {
                    Trace.Listeners.Remove(_xunitTraceListener);
                }
            }
        }

        protected void WaitForTaskToFinish(Task task, TimeSpan timeout)
        {
            Assert.True(SpinWait.SpinUntil(() => task.IsCompleted, timeout), "Task failed to finish within time limit.");
        }

        protected Task StartNewTask(Action action)
        {
            var task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(t => { var error = t.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            Assert.True(SpinWait.SpinUntil(() => !task.IsWaitingToRun(), TimeSpan.FromMinutes(5)), "Failed to start task within time limit.");
            return task;
        }

        protected Task<T> StartNewTask<T>(Func<T> action)
        {
            var task = Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            task.ContinueWith(t => { var error = t.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            Assert.True(SpinWait.SpinUntil(() => !task.IsWaitingToRun(), TimeSpan.FromMinutes(5)), "Failed to start task within time limit.");
            return task;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}