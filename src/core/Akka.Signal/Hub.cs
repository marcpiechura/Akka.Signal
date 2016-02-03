using Akka.Actor;
using Akka.IO;
using System.Collections.Generic;

namespace Akka.Signal
{
    public class Hub : ReceiveActor
    {
        private readonly IActorRef _handler;
        private readonly Dictionary<string, IActorRef> _clientDictionary = new Dictionary<string, IActorRef>();
        
        public Hub(IActorRef handler)
        {
            _handler = handler;

            Receive<Signal.Join>(join =>
            {
                Sender.Tell(WriteObject(new Signal.Joined(Sender.Path.Name, Self.Path.Name, true)));

                //Client hasn't received the previous joined message so we don't need to forward it again to the other clients
                if (_clientDictionary.ContainsKey(Sender.Path.Name))
                    return;

                var joined = WriteObject(new Signal.Joined(Sender.Path.Name, Self.Path.Name));
                foreach (var client in _clientDictionary.Values)
                    client.Tell(joined);

                _handler.Tell(joined);

                _clientDictionary[Sender.Path.Name] = Sender;
            });

            Receive<Signal.Leave>(leave =>
            {
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
                    foreach (var client in _clientDictionary.Values)
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
