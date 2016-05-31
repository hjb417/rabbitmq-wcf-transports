using System;
using System.ServiceModel;
using System.Threading.Tasks;
using WcfServer;

namespace WcfClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();
            var svc = CreateClientFromConfig();
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
                    Task.Factory.StartNew(() =>
                    {
                        var next = random.Next();
                        Console.WriteLine($"Sending {next}.");
                        Console.WriteLine(client.GetData(next));
                    });
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

        static ChannelFactory<IService1> CreateClientFromConfig()
        {
            return new ChannelFactory<IService1>("Service1");
        }
    }
}
