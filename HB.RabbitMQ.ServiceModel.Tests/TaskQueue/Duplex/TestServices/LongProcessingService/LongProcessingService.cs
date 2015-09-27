using System;
using System.ServiceModel;
using System.Threading;

namespace HB.RabbitMQ.ServiceModel.Tests.TaskQueue.Duplex.TestServices.LongProcessingService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class LongProcessingService : MarshalByRefObject, ILongProcessingService
    {
        private int _processStuffCounter;
        private int _processingStuffCounter;
        
        public LongProcessingService()
        {
            SleepTime = TimeSpan.FromMinutes(3);
        }

        public TimeSpan SleepTime { get; set; }
        public int ProcessingStuffCounter { get { return _processingStuffCounter; } }
        public int ProcessStuffCounter { get { return _processStuffCounter; } }
        
        public override object InitializeLifetimeService()
        {
            return null;
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void ProcessStuff(string stuff)
        {
            Interlocked.Increment(ref _processingStuffCounter);
            Thread.Sleep(SleepTime);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Interlocked.Increment(ref _processStuffCounter);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void Noop()
        {
            Interlocked.Increment(ref _processingStuffCounter);
            Interlocked.Increment(ref _processStuffCounter);
        }
    }
}