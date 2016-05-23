using System;
using System.Web.Hosting;
using HB.RabbitMQ.ServiceModel.Hosting.TaskQueue;
using HB.RabbitMQ.ServiceModel.Tests;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Hosting.Tests.TaskQueue
{
    public class RabbitMQTaskQueueProcessProtocolHandlerTests : UnitTest
    {
        public RabbitMQTaskQueueProcessProtocolHandlerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            Random = new Random();
            AppDomainProtocolHandlerManager = Substitute.For<IAdphManager>();
            ProcessProtocolHandler = new RabbitMQTaskQueueProcessProtocolHandler();
        }

        private RabbitMQTaskQueueProcessProtocolHandler ProcessProtocolHandler { get; }
        private Random Random { get; }
        private Uri WasInteropServiceUri { get; }
        private IAdphManager AppDomainProtocolHandlerManager { get; }
        private const string ProtocolId = "hb.rmqtq";

        private IListenerChannelCallback CreateListenerChannelCallback(int listenerChannelId, string applicationId)
        {
            var setup = new ListenerChannelSetup(applicationId, Guid.NewGuid().ToString(), new Uri($"net.pipe://localhost/{Guid.NewGuid():N}"));
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
        public void StartListenerChannelInvokesStartAppDomainProtocolListenerChannelTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var appId = Guid.NewGuid().ToString();
            var callback = CreateListenerChannelCallback(listenerChannelId, appId);

            ProcessProtocolHandler.StartListenerChannel(callback, AppDomainProtocolHandlerManager);
            AppDomainProtocolHandlerManager.Received(1).StartAppDomainProtocolListenerChannel(appId, ProtocolId, callback);
        }

        [Fact]
        public void StopListenerChannelInvokesStopAppDomainProtocolListenerChannelTest()
        {
            var listenerChannelId = Random.Next(0, 10);
            var appId = Guid.NewGuid().ToString();
            var callback = CreateListenerChannelCallback(listenerChannelId, appId);

            ProcessProtocolHandler.StartListenerChannel(callback, AppDomainProtocolHandlerManager);
            ProcessProtocolHandler.StopListenerChannel(listenerChannelId, true);
            AppDomainProtocolHandlerManager.Received(1).StopAppDomainProtocolListenerChannel(appId, ProtocolId, listenerChannelId, true);
        }
    }
}