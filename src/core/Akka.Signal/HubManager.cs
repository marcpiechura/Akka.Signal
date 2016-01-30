using System;
using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.IO;

namespace Akka.Signal
{
    public sealed class HubManager : ReceiveActor, IWithUnboundedStash
    {
        public const string Name = "AkkaSignalHub";
        public const string ConnecteionPrefix = "~connection";
        
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private static readonly Func<IActorRef, string> GetConnectionNameFromActor =
            actor => $"{ConnecteionPrefix}_{actor.Path.Name}";

        public IStash Stash { get; set; }

        public HubManager()
        {
            Receive<Signal.Bind>(bind => Become(Binding(bind.EndPoint, Sender)));
            ReceiveAny(_ => Stash.Stash());
        }

        private Receive Binding(EndPoint endPoint, IActorRef sender)
        {
            Context.System.Tcp().Tell(new Tcp.Bind(Self, endPoint));

            return message =>
            {
                var failed = message as Tcp.CommandFailed;
                if (failed != null)
                {
                    _log.Error($"Could not bind to endpoint {endPoint}. Reason: {failed.Cmd.FailureMessage}");
                    sender.Tell(new Signal.BindingFailed(endPoint, failed.Cmd.FailureMessage.ToString()));
                    Context.Stop(Self);
                    return true;
                }

                var bound = message as Tcp.Bound;
                if (bound != null)
                {
                    _log.Info($"Listening on {bound.LocalAddress}");
                    sender.Tell(new Signal.Bound(endPoint));
                    Become(Bound);
                    return true;
                }

                Stash.Stash();
                return true;
            };

        }

        private void Bound()
        {
            Receive<Signal.StartHub>(hub =>
            {
                var child = Context.Child(hub.HubName);

                if (!child.IsNobody())
                {
                    Sender.Tell(new Signal.HubAlreadyExists(hub.HubName, child));
                    return;
                }

                _log.Info($"Hub {hub.HubName} created");
                var newHub = Context.ActorOf(Props.Create(() => new Hub(Sender)), hub.HubName);
                Sender.Tell(new Signal.HubStarted(newHub, hub.HubName));
            });

            Receive<Tcp.Connected>(connected =>
            {
                _log.Info($"New client connection {Sender.Path.Name} established. Remote: {connected.RemoteAddress} -> Local: {connected.LocalAddress}");

                var hubConncetion = Context.ActorOf(Props.Create(() => new HubConnection(Context)),
                    GetConnectionNameFromActor(Sender));
                Sender.Tell(new Tcp.Register(hubConncetion));
            });

            Receive<Tcp.Received>(received =>
            {
                var hubConnection = Context.Child(GetConnectionNameFromActor(Sender));
                if (hubConnection.IsNobody())
                    return;
                
                hubConnection.Forward(received);
            });

            Stash.UnstashAll();
        }


    }
}
