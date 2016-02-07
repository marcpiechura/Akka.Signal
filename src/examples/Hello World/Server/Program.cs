using System;
using Akka.Actor;
using Akka.IO;
using Akka.Signal;
using System.Collections.Generic;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sys = ActorSystem.Create("Server"))
            {
                var myhub = sys.ActorOf(Props.Create(() => new Hub()));

                sys.SignalHub().Tell(new Signal.Bind(5678));
                sys.SignalHub().Tell(new Signal.StartHub("Test"), myhub);
                
                sys.WhenTerminated.Wait();
            }
        }
    }

    class Hub : ReceiveActor
    {
        private IActorRef _hub;
        private HashSet<string> _clients;
        private int i = 0;

        public Hub()
        {
            Receive<Signal.HubStarted>(started =>
            {
                _hub = started.Hub;
                _clients = new HashSet<string>();
                Become(Started);
            });
        }

        private void Started()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(1000, 1000, Self, "", Nobody.Instance);

            Receive<string>(_ =>
            {
                Send(DateTime.Now.ToLongTimeString()); //broadcast to all
                foreach (var client in _clients)
                    Send(i++, client); //broadcast to single, specific client
            });

            Receive<Signal.Joined>(x => ClientJoined(x.Client));
            Receive<Signal.Left>(x => ClientLeft(x.Client));
            Receive<Signal.ClientBroadcast>(x => Received(x.Client, x.Message));
        }

        private void ClientJoined(string clientId)
        {
            if (_clients.Add(clientId))
                Console.WriteLine($"Client {clientId} Joined.");
        }

        private void ClientLeft(string clientId)
        {
            if (_clients.Remove(clientId))
                Console.WriteLine($"Client {clientId} Left.");
        }

        private void Received(string clientId, object message) => Console.WriteLine($"Message from {clientId}: {message?.ToString()}");

        private void Send(object message, string clientId = null) => _hub.Tell(new Signal.Broadcast(null, message, clientId));
    }
}
