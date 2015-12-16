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
                var client = sys.ActorOf(Props.Create(() => new HubClient("localhost", 5678)));
                var console = sys.ActorOf(Props.Create(() => new ConsoleActor()));
                client.Tell(new Register(console));

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
