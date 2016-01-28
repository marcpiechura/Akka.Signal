using System.Net;
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

            Receive<Signal.Join>(join =>
            {
                if (!_clients.TryAdd(Sender, out _clients))
                    return;

                var joined = new Signal.Joined(Sender.Path.Name, Self.Path.Name);
                Self.Tell(new Signal.Broadcast("self", joined));

                _handler.Tell(joined);
            });

            Receive<Signal.Leave>(leave =>
            {
                if (!_clients.TryRemove(Sender, out _clients))
                    return;

                var left = new Signal.Left(Sender.Path.Name, Self.Path.Name);
                Self.Tell(new Signal.Broadcast("self", left));

                _handler.Tell(left);
            });

            Receive<Signal.Broadcast>(broadcast =>
            {
                var message = WriteObject(broadcast.Message);
                foreach (var client in _clients)
                    client.Tell(message);
            });

            ReceiveAny(o => Self.Forward(new Signal.Broadcast("self", o)));
        }

        private static Tcp.Write WriteObject(object value) => Tcp.Write.Create(MessageSeriliazer.Serialize(Context.System, value));
    }
}
