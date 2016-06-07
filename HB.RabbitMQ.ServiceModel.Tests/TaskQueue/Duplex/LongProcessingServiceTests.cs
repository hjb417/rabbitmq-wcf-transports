using System;
using System.Diagnostics;
using System.Linq;
using System.Messaging;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex.TestServices.LongProcessingService;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex
{
    public abstract class LongProcessingServiceTests : UnitTest
    {
        private bool _unloadClientAppDomain = true;
        private bool _unloadServerAppDomain = true;

        private readonly TimeSpan _serverCloseTimeOut = TimeSpan.FromSeconds(6);

        //private readonly TimeSpan _processSleepTime = TimeSpan.FromMinutes(3);
        //private readonly TimeSpan _serverCloseTimeOut = TimeSpan.FromMinutes(3.25);

        private readonly TimeSpan _clientCloseTime = TimeSpan.FromSeconds(1.5);
        private readonly TimeSpan _clientAbortTime = TimeSpan.FromSeconds(1.5);
        private readonly TimeSpan _serverAbortTime = TimeSpan.FromSeconds(1.5);

        private readonly string _clientQueueName;

        protected LongProcessingServiceTests(BindingTypes bindingType, ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            Server = ServiceFactory.CreateServer<ILongProcessingService, LongProcessingService>(bindingType, false);
            Server.Service.SleepTime = TimeSpan.FromSeconds(3);

            Uri clientUri = null;
            if (bindingType == BindingTypes.DuplexMsmq)
            {
                _clientQueueName = typeof(ILongProcessingService).Name + "_Client-" + Guid.NewGuid();
                MessageQueue.Create(@".\private$\" + _clientQueueName);
                clientUri = new Uri("net.msmq://localhost/private/" + _clientQueueName);
            }
            Client = ServiceFactory.CreateClient<LongProcessingServiceClient>(bindingType, Server.ServiceUri, clientUri);
        }

        protected SelfHostedService<ILongProcessingService, LongProcessingService> Server { get; private set; }
        protected LongProcessingServiceClient Client { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_unloadServerAppDomain)
                {
                    try
                    {
                        Server.ServiceAppDomain.Unload();
                    }
                    catch { }
                }
                if (_unloadClientAppDomain)
                {
                    try
                    {
                        Client.ServiceAppDomain.Unload();
                    }
                    catch { }
                }
                if (_clientQueueName != null)
                {
                    MessageQueue.Delete(@".\private$\" + _clientQueueName);
                }
            }
            base.Dispose(disposing);
        }

        private void WaitForServerToReceiveMessage()
        {
            Assert.True(SpinWait.SpinUntil(() => Server.Service.ProcessingStuffCounter > 0, TimeSpan.FromSeconds(30)), "The server has not received the message in the alloted time.");
        }

        #region server close

        [Fact]
        public void ClientCloseServerCloseTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());
            var closeClientTask = StartNewTask(() => Client.Close(TimeSpan.MaxValue));
            Assert.True(closeClientTask.Wait(_clientCloseTime));
            Assert.False(closeClientTask.IsFaulted);
            Assert.All(Client.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            WaitForServerToReceiveMessage();

            //all server objects should be open.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Opened, s.State));

            var timer = Stopwatch.StartNew();
            var closeServerTask = StartNewTask(() => Server.Close(TimeSpan.MaxValue));
            closeServerTask.Wait(_serverCloseTimeOut);
            timer.Stop();

            //all server objects should be closed.
            Assert.All(Server.GetStates(), s => Assert.True(CommunicationState.Closed == s.State, $"Server object of type [{s.Type}] is {s.State} but should be closed."));

            //service should have closed w/o error.
            Assert.True(closeServerTask.IsCompleted);
            Assert.False(closeServerTask.IsFaulted);

            //service method should have ran to completion.
            Assert.Equal(1, Server.Service.ProcessStuffCounter);

            //the close method should have waited for the service call to complete before returning.
            Assert.True(timer.Elapsed >= Server.Service.SleepTime);
            Assert.True(timer.Elapsed <= _serverCloseTimeOut);

            //all server objects should be closed.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));
        }

        [Fact]
        //TODO: Fix this. The server object is still open after the client aborts
        public void ClientAbortServerCloseTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());
            var abortClientTask = StartNewTask(Client.Abort);
            Assert.True(abortClientTask.Wait(_clientAbortTime));
            Assert.False(abortClientTask.IsFaulted);
            Assert.All(Client.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            WaitForServerToReceiveMessage();

            //HACK: The interval between the TryReceive is between 4-5 seconds.. I can't explain it
            //WCF is calling TryReceive to pull in messages.
            Thread.Sleep(TimeSpan.FromSeconds(5));

            //all server objects should be open.
            Assert.All(Server.GetStates<IChannelListener>(), s => Assert.Equal(CommunicationState.Opened, s.State));
            Assert.All(Server.GetStates<IChannel>(), s => Assert.Equal(CommunicationState.Closed, s.State)); // <-- fails here

            var closeServerTask = StartNewTask(() => Server.Close(TimeSpan.MaxValue));
            closeServerTask.Wait(_serverCloseTimeOut);

            //service should still be running.
            Assert.True(closeServerTask.IsCompleted);

            Assert.Equal(1, Server.Service.ProcessStuffCounter);

            //all server objects should be closed.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));
        }

        [Fact]
        //The server shouldn't close because it needs to ensure the client has closed.
        public void ClientTerminateServerCloseTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());
            Client.ServiceAppDomain.Unload();
            _unloadClientAppDomain = false;

            WaitForServerToReceiveMessage();

            //all server objects should be open.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Opened, s.State));

            var closeServerTask = StartNewTask(() => Server.Close(TimeSpan.MaxValue));
            Stopwatch timer = Stopwatch.StartNew();
            closeServerTask.Wait(_serverCloseTimeOut);
            timer.Stop();

            //service should have executed
            Assert.Equal(1, Server.Service.ProcessStuffCounter);

            //all server objects should be closed or closing
            Assert.All(Server.GetStates(), s => Assert.True(s.ContainsState(CommunicationState.Closed, CommunicationState.Closing)));

            //service should still be executing
            Assert.False(closeServerTask.IsCompleted);
        }

        #endregion

        #region server abort

        [Fact]
        public void ClientCloseServerAbortTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());
            var closeClientTask = StartNewTask(() => Client.Close(TimeSpan.MaxValue));
            Assert.True(closeClientTask.Wait(_clientCloseTime));
            Assert.False(closeClientTask.IsFaulted);
            Assert.All(Client.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            WaitForServerToReceiveMessage();

            //all server objects should be open.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Opened, s.State));

            var timer = Stopwatch.StartNew();
            var abortServerTask = StartNewTask(Server.Abort);
            abortServerTask.Wait(_serverAbortTime);
            timer.Stop();

            Console.WriteLine(timer.Elapsed);
            Trace.WriteLine(timer.Elapsed);

            //service should have aborted w/o error.
            Assert.True(abortServerTask.IsCompleted);
            Assert.False(abortServerTask.IsFaulted);

            //all server objects should be closed.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            //service method should not have ran to completion.
            Assert.Equal(0, Server.Service.ProcessStuffCounter);

            //the abort should return almost immediately.
            Assert.True(timer.Elapsed <= _serverAbortTime);
        }

        [Fact]
        //I think this test is failing because the RabbitMQ version is not processing results asyncronosuly.
        //It's waiting for the method ProcessStuff to finish before aborting.
        public void ClientAbortServerAbortTest()
        {
            Server.Service.SleepTime = TimeSpan.FromDays(15);

            Client.ProcessStuff(Guid.NewGuid().ToString());
            Client.ProcessStuff(Guid.NewGuid().ToString());

            var abortClientTask = StartNewTask(Client.Abort);
            Assert.True(abortClientTask.Wait(_clientAbortTime));
            Assert.False(abortClientTask.IsFaulted);
            Assert.All(Client.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            WaitForServerToReceiveMessage();

            Thread.Sleep(TimeSpan.FromSeconds(5));

            //all server objects should be open.
            Assert.All(Server.GetStates<IChannelListener>(), s => Assert.Equal(CommunicationState.Opened, s.State));
            Assert.All(Server.GetStates<IChannel>(), s => Assert.True(CommunicationState.Closed == s.State, $"Object of type [{s.Type}] is [{s.State}]."));

            var timer = Stopwatch.StartNew();
            var abortServerTask = StartNewTask(Server.Abort);
            abortServerTask.Wait(_serverAbortTime);
            timer.Stop();

            Console.WriteLine(timer.Elapsed);
            Trace.WriteLine(timer.Elapsed);

            //service should have aborted w/o error.
            Assert.True(abortServerTask.IsCompleted);
            Assert.False(abortServerTask.IsFaulted);

            //all server objects should be closed.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Closed, s.State));

            //service method should not have ran to completion.
            Assert.Equal(0, Server.Service.ProcessStuffCounter);

            //it should only be processing one request at a time.
            Assert.Equal(1, Server.Service.ProcessingStuffCounter);

            //the abort should return almost immediately.
            Assert.True(timer.Elapsed <= _serverAbortTime);
        }

        [Fact]
        public void ClientTerminateServerAbortTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());
            Client.ServiceAppDomain.Unload();
            _unloadClientAppDomain = false;

            WaitForServerToReceiveMessage();

            //all server objects should be open.
            Assert.All(Server.GetStates(), s => Assert.Equal(CommunicationState.Opened, s.State));

            var timer = Stopwatch.StartNew();
            var abortServerTask = StartNewTask(Server.Abort);
            abortServerTask.Wait(_serverAbortTime);
            timer.Stop();

            //all server objects should be closed
            Assert.All(Server.GetStates(), s => Assert.True(CommunicationState.Closed == s.State, $"Server object of type [{s.Type}] is {s.State} but should be closed."));

            //service should be aborted w/o error
            Assert.True(abortServerTask.IsCompleted);
            Assert.False(abortServerTask.IsFaulted);

            //service should not have executed
            Assert.Equal(0, Server.Service.ProcessStuffCounter);

            Assert.True(timer.Elapsed <= _serverAbortTime);
        }

        [Fact]
        public void ClientCloseDoesNotWaitForServiceToProcessRequestTest()
        {
            Client.ProcessStuff(Guid.NewGuid().ToString());

            WaitForServerToReceiveMessage();

            var timer = Stopwatch.StartNew();
            var closeClientTask = StartNewTask(() => Client.Close(TimeSpan.MaxValue));
            closeClientTask.Wait(_clientCloseTime);
            timer.Stop();

            Assert.True(closeClientTask.IsCompleted);
            Assert.False(closeClientTask.IsFaulted);
            Assert.Equal(0, Server.Service.ProcessStuffCounter);
        }

        [Fact]
        public void ClientCloseClosesServerChannelTest()
        {
            Client.Noop();
            WaitForServerToReceiveMessage();

            var timer = Stopwatch.StartNew();
            var closeClientTask = StartNewTask(() => Client.Close(TimeSpan.MaxValue));
            closeClientTask.Wait(_clientCloseTime);
            timer.Stop();

            Assert.True(closeClientTask.IsCompleted);
            Assert.False(closeClientTask.IsFaulted);


            //add some delay to allow the channels to close for rabbitmq.
            Thread.Sleep(TimeSpan.FromSeconds(3));

            Assert.All(Server.GetStates<IChannel>(), s => Assert.True(s.State == CommunicationState.Closed, $"The communication object [{s.Type}] is in the [{s.State}] state."));
            Assert.Equal(1, Server.GetStates<IChannel>().Count());
        }

        #endregion
    }
}