using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.IO;

namespace Akka.Signal
{
    public class HubConnection : ReceiveActor
    {
        private readonly IUntypedActorContext _parentContext;
        private Dictionary<string, IActorRef> _hubs = new Dictionary<string, IActorRef>();

        private readonly ILoggingAdapter _log = Context.GetLogger();

        public HubConnection(IUntypedActorContext parentContext)
        {
            _parentContext = parentContext;
            
            Receive<Tcp.Received>(received =>
            {
                var messages = MessageSeriliazer.Deserialize(Context.System, received.Data);
                messages.ForEach(Self.Forward);
            });

            Receive<Tcp.ConnectionClosed>(closed =>
            {
                _log.Info($"Client connection {Self.Path.Name} was closed. Reason: {closed.GetErrorCause()}");

                foreach (var hubPair in _hubs)
                {
                    var name = hubPair.Key;
                    var hub = hubPair.Value;

                    hub.Forward(new Signal.Leave(name));
                }

                Context.Stop(Self);
            });

            Receive<Signal.Join>(join =>
            {
                if (_hubs.ContainsKey(join.HubName))
                    return;

                var hub = _parentContext.Child(join.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Signal.NotFound(join.HubName)));
                    return;
                }

                _hubs.Add(join.HubName, hub);
                hub.Forward(join);
            });

            Receive<Signal.Leave>(leave =>
            {
                if (!_hubs.ContainsKey(leave.HubName))
                    return;

                var hub = _parentContext.Child(leave.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Signal.NotFound(leave.HubName)));
                    return;
                }

                _hubs.Remove(leave.HubName);
                hub.Forward(leave);
            });

            Receive<Signal.Broadcast>(broadcast =>
            {
                var hub = _parentContext.Child(broadcast.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Signal.NotFound(broadcast.HubName)));
                    return;
                }

                hub.Forward(new Signal.ClientBroadcast(Sender.Path.Name, broadcast.Message));
            });
        }

        private static Tcp.Write WriteObject(object value) => Tcp.Write.Create(MessageSeriliazer.Serialize(Context.System, value));
    }
}
