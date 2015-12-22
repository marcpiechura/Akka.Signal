using Akka.Actor;
using Akka.Event;
using Akka.IO;
using Akka.Util.Internal.Collections;

namespace Akka.Signal
{
    public class HubConnection : ReceiveActor
    {
        private readonly IUntypedActorContext _parentContext;
        private IImmutableMap<string, IActorRef> _hubs = ImmutableTreeMap<string, IActorRef>.Empty;

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

                foreach (var hubPair in _hubs.AllMinToMax)
                {
                    var name = hubPair.Key;
                    var hub = hubPair.Value;

                    hub.Forward(new Hub.Leave(name));
                }

                Context.Stop(Self);
            });

            Receive<Hub.Join>(join =>
            {
                if (_hubs.Contains(join.HubName))
                    return;

                var hub = _parentContext.Child(join.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Hub.NotFound(join.HubName)));
                    return;
                }

                _hubs = _hubs.Add(join.HubName, hub);
                hub.Forward(join);
            });

            Receive<Hub.Leave>(leave =>
            {
                if (!_hubs.Contains(leave.HubName))
                    return;

                var hub = _parentContext.Child(leave.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Hub.NotFound(leave.HubName)));
                    return;
                }

               _hubs = _hubs.Remove(leave.HubName);
                hub.Forward(leave);
            });

            Receive<Hub.Broadcast>(broadcast =>
            {
                var hub = _parentContext.Child(broadcast.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Hub.NotFound(broadcast.HubName)));
                    return;
                }

                hub.Forward(broadcast);
            });
        }

        private static Tcp.Write WriteObject(object value) => Tcp.Write.Create(MessageSeriliazer.Serialize(Context.System, value));
    }
}
