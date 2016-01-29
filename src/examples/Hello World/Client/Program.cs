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
            using (var sys = ActorSystem.Create("Signal"))
            {
                sys.SignalClient().Tell(new Signal.Bind(new DnsEndPoint("localhost", 5678)));
                var console = sys.ActorOf(Props.Create(() => new ConsoleActor()));
                
                sys.SignalClient().Tell(new Signal.RegisterClient("Test"), console);

                sys.WhenTerminated.Wait();
            }
        }
    }


    class ConsoleActor : ReceiveActor
    {
        private string _ownId;

        public ConsoleActor()
        {
            Receive<Signal.Connecting>(connecting => Console.WriteLine("Connecting to server..."));
            Receive<Signal.Connected>(connected => Console.WriteLine("Successfully connected"));
            Receive<Signal.Disconnected>(disconnected =>
            {
                _ownId = string.Empty;
                Console.WriteLine("Connection to the server got lost...");
            });
            Receive<Signal.Reconnecting>(reconnecting => Console.WriteLine("Reconnecting to server..."));
            Receive<Signal.Reconnected>(reconnected => Console.WriteLine("Successfully reconnected to the server"));
            Receive<Signal.ClientRegistered>(
                registered =>
                    Console.WriteLine(
                        $"Client successfully registered for hub {registered.Hub} on connection {registered.Client}"));
            Receive<Signal.Joined>(
                joined =>
                {
                    if (string.IsNullOrWhiteSpace(_ownId))
                    {
                        _ownId = joined.Client;
                        Console.WriteLine($"Successfully joined {joined.HubName} with clientId {_ownId}, we now receive updates from this hub");
                    }
                    else
                        Console.WriteLine($"A new client has joined the hub {joined.HubName} with clientId {joined.Client}");
                });
            Receive<Signal.Left>(left => Console.WriteLine($"The client {left.Client} hast left the hub {left.Hub}"));
            ReceiveAny(Console.WriteLine);
        }
    }
}
