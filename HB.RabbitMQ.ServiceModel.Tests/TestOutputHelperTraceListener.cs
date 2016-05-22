using System.Diagnostics;
using Xunit.Abstractions;

namespace HB.RabbitMQ.ServiceModel.Tests
{
    internal sealed class TestOutputHelperTraceListener : TraceListener
    {
        private readonly ITestOutputHelper _outputHelper;

        public TestOutputHelperTraceListener(ITestOutputHelper testOutputHelper)
        {
            _outputHelper = testOutputHelper;
        }

        public override void Write(string message)
        {
            _outputHelper.WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            _outputHelper.WriteLine(message);
        }
    }
}
