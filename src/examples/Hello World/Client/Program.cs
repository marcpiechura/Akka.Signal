using System;
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
                var console = sys.ActorOf(Props.Create(() => new ConsoleActor()));
                for (int i = 0; i < 100; i++)
                {
                    var client = sys.ActorOf(Props.Create(() => new HubClient("localhost", 5678)));
                    client.Tell(new Register(console));

                }
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
