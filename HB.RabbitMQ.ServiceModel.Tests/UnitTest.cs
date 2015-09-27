﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    public abstract class UnitTest : IDisposable
    {
        protected UnitTest()
        {
        }

        ~UnitTest()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected Task StartNewTask(Action action)
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