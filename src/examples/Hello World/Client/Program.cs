using System;
using System.Net;
using System.Threading.Tasks;
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
                var myclient = sys.ActorOf(Props.Create(() => new MyClient()));
                sys.SignalClient().Tell(new Signal.Bind(new DnsEndPoint("localhost", 5678)));
                sys.SignalClient().Tell(new Signal.RegisterClient("Test"), myclient);
                sys.WhenTerminated.Wait();
            }
        }
    }

    public class MyClient : ReceiveActor
    {
        private string _ownId;
        private IActorRef _client;

        private string Id => _ownId;

        public MyClient()
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
            Receive<Signal.ClientRegistered>(registered =>
            {
                _client = registered.Client;
                Console.WriteLine($"Client successfully registered for hub {registered.Hub} on connection {_client}");
            });
            Receive<Signal.Joined>(
                joined =>
                {
                    _ownId = joined.Client;
                    Console.WriteLine($"Successfully joined {joined.HubName} with clientId {_ownId}, we now receive updates from this hub");
                    OnJoined(_ownId, joined.HubName);
                }, joined => joined.Self);
            Receive<Signal.Joined>(joined => Console.WriteLine($"A new client has joined the hub {joined.HubName} with clientId {joined.Client}"), joined => !joined.Self);
            Receive<Signal.Left>(left => Console.WriteLine($"The client {left.Client} hast left the hub {left.Hub}"));
            ReceiveAny(OnReceived);
        }

        private void OnJoined(string clientId, string hubName) => Console.WriteLine($"Joined {hubName} as id {clientId}");

        private void OnReceived(object message)
        {
            Console.WriteLine(message);
            Send($"ack from client: {Id}, length: {message?.ToString()?.Length ?? 0}");
        }

        private void Send(object message) => _client?.Tell(message);
    }
}