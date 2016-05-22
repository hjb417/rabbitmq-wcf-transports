using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Web.Hosting;
using HB.RabbitMQ.ServiceModel.Hosting.ServiceModel;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue.WasInterop;
using HB.RabbitMQ.ServiceModel.Tests;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Hosting.Tests.TaskQueue
{
    public class RabbitMQTaskQueueAppDomainProtocolHandlerTests : UnitTest
    {
        public RabbitMQTaskQueueAppDomainProtocolHandlerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            Random = new Random();
            AppDomainProtocolHandler = new RabbitMQTaskQueueAppDomainProtocolHandler();
            WasInteropService = Substitute.For<IWasInteropService>();
            WasInteropServiceUri = new Uri($"net.pipe://localhost/{Guid.NewGuid():N}");

            WasInteropServiceHost = new ServiceHost(WasInteropService, WasInteropServiceUri);
            WasInteropServiceHost.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;
            WasInteropServiceHost.Description.Behaviors.Find<ServiceBehaviorAttribute>().InstanceContextMode = InstanceContextMode.Single;
            WasInteropServiceHost.AddServiceEndpoint(typeof(IWasInteropService), NetNamedPipeBindingFactory.Create(), string.Empty);
            WasInteropServiceHost.Open();
        }

        private RabbitMQTaskQueueAppDomainProtocolHandler AppDomainProtocolHandler { get; }
        private Random Random { get; }
        private IWasInteropService WasInteropService { get; }
        private ServiceHost WasInteropServiceHost { get; }
        private Uri WasInteropServiceUri { get; }

        private IListenerChannelCallback CreateListenerChannelCallback(int listenerChannelId, string applicationPath)
        {
            var setup = new ListenerChannelSetup(Guid.NewGuid().ToString(), applicationPath, WasInteropServiceUri);
            var blob = setup.ToBytes();
            var callback = Substitute.For<IListenerChannelCallback>();
            var bufferSize = 0;
            callback.GetId().Returns(listenerChannelId);
            callback.GetBlobLength().Returns(blob.Length);
            callback.WhenForAnyArgs(x => x.GetBlob(null, ref bufferSize)).Do(x =>
            {
                Array.Copy(blob, x.ArgAt<Array>(0), blob.Length);
                x[1] = blob.Length;
            });
            return callback;
        }

        [Fact]
        public void StartListenerChannelRegistersWithWasInteropServiceTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var appPath = Guid.NewGuid().ToString();
            var callback = CreateListenerChannelCallback(listenerChannelId, appPath);
            AppDomainProtocolHandler.StartListenerChannel(callback);
            WasInteropService.Received(1).Register(listenerChannelId, appPath);
        }

        [Fact]
        public void StartListenerChannelInvokesReportStartedMethodTest()
        {
            var callback = CreateListenerChannelCallback(Random.Next(), Guid.NewGuid().ToString());
            AppDomainProtocolHandler.StartListenerChannel(callback);
            callback.Received(1).ReportStarted();
        }

        [Fact]
        public void StopListenerChannelUnregistersWithAsInteropServiceTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var callback = CreateListenerChannelCallback(listenerChannelId, Guid.NewGuid().ToString());
            AppDomainProtocolHandler.StartListenerChannel(callback);
            AppDomainProtocolHandler.StopListenerChannel(listenerChannelId, true);
            WasInteropService.Received(1).Unregister(listenerChannelId);
        }

        [Fact]
        public void StopListenerChannelInvokesReportStoppedTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var callback = CreateListenerChannelCallback(listenerChannelId, Guid.NewGuid().ToString());
            AppDomainProtocolHandler.StartListenerChannel(callback);
            AppDomainProtocolHandler.StopListenerChannel(listenerChannelId, true);
            callback.Received(1).ReportStopped(0);
        }

        [Fact]
        public void StopProtocolReportStoppedTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var callback = CreateListenerChannelCallback(listenerChannelId, Guid.NewGuid().ToString());
            AppDomainProtocolHandler.StartListenerChannel(callback);
            AppDomainProtocolHandler.StopProtocol(true);
            callback.Received(1).ReportStopped(0);
        }

        protected override void Dispose(bool disposing)
        {
            bool abort = true;
            if (disposing)
            {
                try
                {
                    Trace.TraceWarning($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandlerTests)}.{nameof(Dispose)}: The service is being aborted.");
                    WasInteropServiceHost.Close();
                    abort = false;
                }
                catch (Exception e) when (e is TimeoutException || e is CommunicationException)
                {
                }
                finally
                {
                    if (abort)
                    {
                        Trace.TraceWarning($"{nameof(RabbitMQTaskQueueAppDomainProtocolHandlerTests)}.{nameof(Dispose)}: The service is being aborted.");
                        WasInteropServiceHost.Abort();
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}