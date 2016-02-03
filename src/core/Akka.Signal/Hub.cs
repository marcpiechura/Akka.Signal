using System.Net;
using Akka.Actor;
using Akka.IO;
using Akka.Util.Internal.Collections;
using System.Collections.Generic;

namespace Akka.Signal
{
    public class Hub : ReceiveActor
    {
        private readonly IActorRef _handler;
        private IImmutableSet<IActorRef> _clients = ImmutableTreeSet<IActorRef>.Empty;
        private Dictionary<string, IActorRef> _clientDictionary = new Dictionary<string, IActorRef>();


        public Hub(IActorRef handler)
        {
            _handler = handler;

            Receive<Signal.Join>(join =>
            {
                if (!_clients.TryAdd(Sender, out _clients))
                    return;

                _clientDictionary[Sender.Path.Name] = Sender;

                var joined = new Signal.Joined(Sender.Path.Name, Self.Path.Name);
                Self.Tell(new Signal.Broadcast("self", joined));

                _handler.Tell(joined);
            });

            Receive<Signal.Leave>(leave =>
            {
                if (!_clients.TryRemove(Sender, out _clients))
                    return;

                if (_clientDictionary.ContainsKey(Sender.Path.Name))
                    _clientDictionary.Remove(Sender.Path.Name);

                var left = new Signal.Left(Sender.Path.Name, Self.Path.Name);
                Self.Tell(new Signal.Broadcast("self", left));

                _handler.Tell(left);
            });

            Receive<Signal.Broadcast>(broadcast =>
            {
                var message = WriteObject(broadcast.Message);
                if (broadcast.ClientId == null)
                {
                    foreach (var client in _clients)
                        client.Tell(message);
                }
                else if (_clientDictionary.ContainsKey(broadcast.ClientId))
                {
                    _clientDictionary[broadcast.ClientId].Tell(message);
                }
            });

            Receive<Signal.ClientBroadcast>(broadcast => _handler.Tell(broadcast));
        }

        private static Tcp.Write WriteObject(object value) => Tcp.Write.Create(MessageSeriliazer.Serialize(Context.System, value));
    }
}
