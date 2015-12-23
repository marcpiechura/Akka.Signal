using System.Net;
using Akka.Actor;
using Akka.Event;

namespace Akka.Signal
{
    public class ClientManager : ReceiveActor, IWithUnboundedStash
    {
        public const string Name = "AkkaSignalClient";
        private EndPoint _endPoint;

        public IStash Stash { get; set; }

        public ClientManager()
        {
            Receive<Bind>(bind =>
            {
                _endPoint = bind.EndPoint;
                Become(Bound);
            });
            ReceiveAny(_ => Stash.Stash());
        }

        private void Bound()
        {
            Receive<RegisterClient>(registerClient =>
            {
                var newClient = Context.ActorOf(Props.Create(() => new HubClient(_endPoint, Sender, registerClient.Hub)));
                Sender.Tell(new ClientRegistered(newClient, registerClient.Hub));
            });

            Stash.UnstashAll();
        }

        public class Bind
        {
            public Bind(EndPoint endPoint)
            {
                EndPoint = endPoint;
            }

            public EndPoint EndPoint { get; private set; }
        }
        
        public class RegisterClient
        {
            public RegisterClient(string hub)
            {
                Hub = hub;
            }

            public string Hub { get; private set; }
        }

        public class ClientRegistered
        {
            public ClientRegistered(IActorRef client, string hub)
            {
                Client = client;
                Hub = hub;
            }

            public IActorRef Client { get; private set; }

            public string Hub { get; private set; }
        }
    }
}