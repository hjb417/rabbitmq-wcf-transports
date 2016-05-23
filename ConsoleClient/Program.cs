using System;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Transactions;
using Contracts;
using HB.RabbitMQ.ServiceModel.TaskQueue;

namespace ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var svc = CreateClientFromConfig(new SimpleServiceCallback());
            try
            {
                svc.Open();
                var client = svc.CreateChannel();
                Console.WriteLine("Client Ready!");
                while (true)
                {
                    var txt = Console.ReadLine();
                    if (txt == "quit")
                    {
                        svc.Close(TimeSpan.MaxValue);
                        break;
                    }
                    if (txt == "abort")
                    {
                        svc.Abort();
                        break;
                    }
                    //Task.Factory.StartNew(() => client.OneWayEcho(txt));
                    Task.Factory.StartNew(() => Console.WriteLine(client.Echo(txt)));
                }
            }
            catch (Exception e)
            {
                return;
            }
            finally
            {
                svc.Close(TimeSpan.MaxValue);
            }
        }

        static ChannelFactory<ISimpleService> CreateClientFromConfig(ISimpleServiceCallback callback)
        {
            return new ChannelFactory<ISimpleService>("SimpleService");
            //return new DuplexChannelFactory<ISimpleService>(new InstanceContext(callback), "SimpleService");
        }

        static ChannelFactory<ISimpleService> CreateClientFromCode(ISimpleServiceCallback callback)
        {
            return new ChannelFactory<ISimpleService>(new RabbitMQTaskQueueBinding(), new EndpointAddress(RabbitMQTaskQueueUri.Create("localhost", 5672, "Contracts.ISimpleService")));
        }
    }

    [ServiceBehavior(TransactionIsolationLevel = IsolationLevel.ReadCommitted)]
    class SimpleServiceCallback : ISimpleServiceCallback
    {
        public void Ack(string input)
        {
            Console.WriteLine("ACK: {0}", input);
        }
    }
}
