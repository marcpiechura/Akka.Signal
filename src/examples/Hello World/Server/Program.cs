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
                var myhub = sys.ActorOf(Props.Create(() => new MyHub()));

                sys.SignalHub().Tell(new Signal.Bind(5678));
                sys.SignalHub().Tell(new Signal.StartHub("Test"), myhub);
                
                sys.WhenTerminated.Wait();
            }
        }
    }
    
    class MyHub : HubActor
    {
        private readonly HashSet<string> _clients = new HashSet<string>();
        private int i = 0;

        protected override void OnStarted()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(1000, 1000, Self, "", Nobody.Instance);

            Receive<string>(_ =>
            {
                Send(DateTime.Now.ToLongTimeString()); //broadcast to all
                foreach (var client in _clients)
                    Send(i++, client); //broadcast to single, specific client
            });
        }

        protected override void OnClientJoined(string clientId)
        {
            Console.WriteLine($"client joined: {clientId}");
            _clients.Add(clientId);
        }

        protected override void OnClientLeft(string clientId)
        {
            Console.WriteLine($"client left: {clientId}");
            _clients.Remove(clientId);
        }

        protected override void OnReceived(string clientId, object message) =>
            Console.WriteLine($"message from {clientId}: {message?.ToString()}");
    }
}
