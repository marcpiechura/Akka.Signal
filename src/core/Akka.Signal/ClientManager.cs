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
            Receive<Signal.Bind>(bind =>
            {
                _endPoint = bind.EndPoint;
                Become(Bound);
            });
            ReceiveAny(_ => Stash.Stash());
        }

        private void Bound()
        {
            Receive<Signal.RegisterClient>(registerClient =>
            {
                var newClient = Context.ActorOf(Props.Create(() => new HubClient(_endPoint, Sender, registerClient.Hub)));
                Sender.Tell(new Signal.ClientRegistered(newClient, registerClient.Hub));
            });

            Stash.UnstashAll();
        }
    }
}