using Akka.Actor;
using Akka.IO;
using Akka.Util.Internal.Collections;

namespace Akka.Signal
{
    public class Hub : ReceiveActor
    {
        private readonly IActorRef _handler;
        private IImmutableSet<IActorRef> _clients = ImmutableTreeSet<IActorRef>.Empty;

        public Hub(IActorRef handler)
        {
            _handler = handler;

            Receive<Join>(join =>
            {
                if (!_clients.TryAdd(Sender, out _clients))
                    return;

                var joined = new Joined(Sender);
                Self.Tell(new Broadcast("self", joined));

                _handler.Tell(joined);
            });

            Receive<Leave>(leave =>
            {
                if (!_clients.TryRemove(Sender, out _clients))
                    return;

                var left = new Left(Sender);
                Self.Tell(new Broadcast("self", left));

                _handler.Tell(left);
            });

            Receive<Broadcast>(broadcast =>
            {
                var message = WriteObject(broadcast.Message);
                foreach (var client in _clients)
                    client.Tell(message);
            });

            ReceiveAny(o => Self.Forward(new Broadcast("self", o)));
        }

        private static Tcp.Write WriteObject(object value) => Tcp.Write.Create(MessageSeriliazer.Seriliaze(Context.System, value));

        public class Join
        {
            public string HubName { get; private set; }

            public Join(string hubName)
            {
                HubName = hubName;
            }
        }

        public class Joined
        {
            public Joined(IActorRef client)
            {
                Client = client;
            }

            public IActorRef Client { get; private set; }
        }

        public class NotFound
        {
            public NotFound(string hubName)
            {
                HubName = hubName;
            }

            public string HubName { get; private set; }
        }

        public class Leave
        {
            public Leave(string hubName)
            {
                HubName = hubName;
            }

            public string HubName { get; private set; }
        }

        public class Left
        {
            public Left(IActorRef client)
            {
                Client = client;
            }

            public IActorRef Client { get; private set; }
        }

        public class Broadcast
        {
            public Broadcast(string hubName, object message)
            {
                HubName = hubName;
                Message = message;
            }

            public string HubName { get; private set; }
            public object Message { get; private set; }
        }
    }
}
