using System;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class ConcurrentOperationManagerTests : UnitTest
    {
        public ConcurrentOperationManagerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            ConcurrentOperationManager = new ConcurrentOperationManager(GetType().FullName);
        }

        private ConcurrentOperationManager ConcurrentOperationManager { get; set; }

        [Fact]
        public void TrackOperationCanBeDisposedMultipleTimesTest()
        {
            var op = ConcurrentOperationManager.TrackOperation();
            for (int i = 0; i < 10; i++)
            {
                op.Dispose();
            }
        }

        [Fact]
        public void OperationContextConstructorThrowsWhenManagerIsDisposedTest()
        {
            ConcurrentOperationManager.OperationContextCreated += delegate 
            {
                StartNewTask(ConcurrentOperationManager.Dispose);
                SpinWait.SpinUntil(() => ConcurrentOperationManager.IsDisposed, TimeSpan.FromSeconds(60));
                Assert.True(ConcurrentOperationManager.IsDisposed);
            };
            Exception error = null;
            try
            {
                var op = ConcurrentOperationManager.TrackOperation();
            }
            catch (ObjectDisposedException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void DisposeBlocksUntilAllOperationsDisposedTest()
        {
            var delay = TimeSpan.FromSeconds(15);
            var op = ConcurrentOperationManager.TrackOperation();
            StartNewTask(() =>
            {
                Thread.Sleep(delay);
                op.Dispose();
            });
            var timer = Stopwatch.StartNew();
            ConcurrentOperationManager.Dispose();
            timer.Stop();
            Assert.True(timer.Elapsed >= delay);
        }

        [Fact]
        public void DisposeReturnsImmediatelyWhenThereAreNoOperationsTest()
        {
            var timer = Stopwatch.StartNew();
            ConcurrentOperationManager.Dispose();
            timer.Stop();
            Assert.True(timer.Elapsed.TotalSeconds < 1);
        }

        [Fact]
        public void DisposeReturnsImmediatelyWhenAllOperationsAreDisposedTest()
        {
            using (ConcurrentOperationManager.TrackOperation())
            using (ConcurrentOperationManager.TrackOperation())
            using (ConcurrentOperationManager.TrackOperation())
            {
            }
            var timer = Stopwatch.StartNew();
            ConcurrentOperationManager.Dispose();
            timer.Stop();
            Assert.True(timer.Elapsed.TotalSeconds < 1);
        }

        [Fact]
        public void TokenCancelledWhenDisposeCalledTest()
        {
            var cancelToken = ConcurrentOperationManager.Token;
            Assert.False(cancelToken.IsCancellationRequested);
            ConcurrentOperationManager.Dispose();
            Assert.True(cancelToken.IsCancellationRequested);
        }

        [Fact]
        public void TrackOperationThrowsWhenManagerIsDisposedTest()
        {
            ConcurrentOperationManager.Dispose();
            Exception error = null;
            try
            {
                ConcurrentOperationManager.TrackOperation();
            }
            catch(ObjectDisposedException e)
            {
                error = e;
            }
            Assert.NotNull(error);
        }

        [Fact]
        public void DisposeCanBeCalledMultipleTimesTest()
        {
            for (int i = 0; i < 10; i++)
            {
                ConcurrentOperationManager.Dispose();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                ConcurrentOperationManager.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}