using System;
using System.Net;
using Akka.Actor;
using Akka.Signal;

namespace Client
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var sys = ActorSystem.Create("Client"))
            {
                sys.SignalClient().Tell(new ClientManager.Bind(new DnsEndPoint("localhost", 5678)));
                var console = sys.ActorOf(Props.Create(() => new ConsoleActor()));

                for (int i = 0; i < 5; i++)
                    sys.SignalClient().Tell(new ClientManager.RegisterClient("aaaa"), console);

                sys.AwaitTermination();
            }
        }
    }


    class ConsoleActor : ReceiveActor
    {
        public ConsoleActor()
        {
            ReceiveAny(Console.WriteLine);
        }
    }
}
