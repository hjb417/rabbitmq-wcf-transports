using System;
using System.Threading;
using Xunit;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public class TimeoutTimerTests : UnitTest
    {
        [Fact]
        public void RemainingTimeIsNeverNegativeTest()
        {
            var timer = TimeoutTimer.StartNew(TimeSpan.FromSeconds(3));
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.Zero, timer.RemainingTime);
        }

        [Fact]
        public void RemainingTimeIsAlwaysMaxValueWhenTimoutIsMaxValueTest()
        {
            var timer = TimeoutTimer.StartNew(TimeSpan.MaxValue);
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.Equal(TimeSpan.MaxValue, timer.RemainingTime);
        }

        [Fact]
        public void HasRemainingTimeReturnsFalseWhenTimeoutReachedTest()
        {
            var timer = TimeoutTimer.StartNew(TimeSpan.FromSeconds(3));
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.False(timer.HasTimeRemaining);
        }

        [Fact]
        public void HasRemainingTimeReturnsTrueWhenTimeoutNotReachedTest()
        {
            var timer = TimeoutTimer.StartNew(TimeSpan.FromSeconds(30));
            SpinWait.SpinUntil(() => false, TimeSpan.FromSeconds(5));
            Assert.True(timer.HasTimeRemaining);
        }

        [Fact]
        public void RemainingTimeDecrementsOnEachCallTest()
        {
            var timer = TimeoutTimer.StartNew(TimeSpan.FromSeconds(15));
            for (var prev = timer.RemainingTime; timer.HasTimeRemaining; prev = timer.RemainingTime)
            {
                Assert.NotEqual(TimeSpan.Zero, prev);
                Assert.True(prev > timer.RemainingTime);
                Thread.Sleep(500);
            }
        }
    }
}