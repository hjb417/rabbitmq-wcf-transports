using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Contracts;
using HB.RabbitMQ.ServiceModel.TaskQueue;
using RabbitMQ.Client;

namespace ConsoleServer
{
    class Program
    {
        internal static volatile bool exit = false;
        static void Main(string[] args)
        {
            //var host = CreateServerFromCode();
            var host = CreateServerFromConfig();
            host.Open();
            try
            {
                Console.WriteLine("Server Ready!");
                while (true)
                {
                    var txt = Console.ReadLine();
                    if (txt == "quit")
                    {
                        exit = true;
                        host.Close(TimeSpan.MaxValue);
                        break;
                    }
                    if (txt == "abort")
                    {
                        host.Abort();
                        break;
                    }
                }
            }
            finally
            {
                //host.Close(TimeSpan.MaxValue);
            }
        }

        static ServiceHost CreateServerFromConfig()
        {
            return new ServiceHost(typeof(SimpleService));
        }

        static ServiceHost CreateServerFromCode()
        {
            var host = new ServiceHost(typeof(SimpleService), RabbitMQTaskQueueUri.Create("Contracts.ISimpleService"));
            host.AddServiceEndpoint(typeof(ISimpleService), new RabbitMQTaskQueueBinding
            {
                ConnectionFactory = new ConnectionFactory
                {
                    HostName = "localhost",
                }
            }, "");
            return host;
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class SimpleService : ISimpleService
    {
        static Process _proc = Process.GetCurrentProcess();

        public string Echo(string input)
        {
            //var callback = OperationContext.Current.GetCallbackChannel<ISimpleServiceCallback>();
            var foo = OperationContext.Current.GetCallbackChannel<IChannel>();
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    return;
                    while (true)
                    {
                        Thread.Sleep(1000);
                        //callback.Ack("WOOT!!!");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });
            var reply = string.Format("{0}-{1}: {2}", DateTimeOffset.Now, _proc.Id, input);
            //Thread.Sleep(TimeSpan.FromSeconds(10));
            Console.WriteLine(reply);
            return reply;
        }

        public void OneWayEcho(string input)
        {
            //var callback = OperationContext.Current.GetCallbackChannel<ISimpleServiceCallback>();
            var foo = OperationContext.Current.GetCallbackChannel<IChannel>();
            var reply = string.Format("{0}-{1}: {2}", DateTimeOffset.Now, _proc.Id, input);
            ThreadPool.QueueUserWorkItem(state =>
            {
                Console.WriteLine(reply);
                return;
                try
                {
                    var objState = foo.State;
                    //while (true)
                    {
                        //callback.Ack(reply);
                        Console.WriteLine("***{0}: {1}", DateTimeOffset.Now, input);
                    }
                }
                catch(Exception e)
                {
                    return;
                }
            });/*
            while(!Program.exit)
            {
                Thread.Sleep(TimeSpan.FromSeconds(15));
            }
            Thread.Sleep(TimeSpan.FromSeconds(15));
            Console.WriteLine("Exiting service method.");*/
            //var reply = string.Format("{0}-{1}: {2}", DateTimeOffset.Now, _proc.Id, input);
            //callback.Ack(reply);
            //Console.WriteLine("***{0}: {1}", DateTimeOffset.Now, input);
        }


        public void Quit()
        {
            return;
        }
    }
}
