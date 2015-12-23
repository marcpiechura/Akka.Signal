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
        private const string ConnecteionPrefix = "~connection";
        
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private static readonly Func<IActorRef, string> GetConnectionNameFromActor =
            actor => $"{ConnecteionPrefix}_{actor.Path.Name}";

        public IStash Stash { get; set; }

        public HubManager()
        {
            Receive<Bind>(bind => Become(Binding(bind.EndPoint, Sender)));
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
                    sender.Tell(new BindingFailed(endPoint, failed.Cmd.FailureMessage.ToString()));
                    Context.Stop(Self);
                    return true;
                }

                var bound = message as Tcp.Bound;
                if (bound != null)
                {
                    _log.Info($"Listening on {bound.LocalAddress}");
                    sender.Tell(new Bound(endPoint));
                    Become(Bounded);
                    return true;
                }

                Stash.Stash();
                return true;
            };

        }

        private void Bounded()
        {
            Receive<StartHub>(hub =>
            {
                var child = Context.Child(hub.HubName);

                if (!child.IsNobody())
                {
                    Sender.Tell(new HubAlreadyExists(hub.HubName, child));
                    return;
                }

                _log.Info($"Hub {hub.HubName} created");
                var newHub = Context.ActorOf(Props.Create(() => new Hub(Sender)), hub.HubName);
                Sender.Tell(new HubStarted(newHub));
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

        public class StartHub
        {
            public StartHub(string hubName)
            {
                if(hubName.StartsWith(ConnecteionPrefix))
                    throw new InvalidActorNameException("The name of a hub MUST not start with '~connection'.");

                HubName = hubName;
            }

            public string HubName { get; private set; }
        }

        public class HubStarted
        {
            public HubStarted(IActorRef hub)
            {
                Hub = hub;
            }

            public IActorRef Hub { get; private set; }
        }

        private class HubAlreadyExists
        {
            public HubAlreadyExists(string name, IActorRef hub)
            {
                Name = name;
                Hub = hub;
            }

            public string Name { get; private set; }

            public IActorRef Hub { get; private set; }
        }

        public class Bind
        {
            public Bind(int port) : this(new IPEndPoint(IPAddress.Any, port))
            {
            }

            public Bind(EndPoint endPoint)
            {
                EndPoint = endPoint;
            }

            public EndPoint EndPoint { get; private set; }
        }

        public class Bound
        {
            public Bound(EndPoint endPoint)
            {
                EndPoint = endPoint;
            }

            public EndPoint EndPoint { get; private set; }
        }

        public class BindingFailed
        {
            public BindingFailed(EndPoint endPoint, string reason)
            {
                EndPoint = endPoint;
                Reason = reason;
            }

            public EndPoint EndPoint { get; private set; }

            public string Reason { get; private set; }
        }
    }
}
