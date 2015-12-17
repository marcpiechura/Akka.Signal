using Akka.Actor;
using Akka.IO;
using Akka.Util.Internal.Collections;

namespace Akka.Signal
{
    public class HubConnection : ReceiveActor
    {
        private readonly IUntypedActorContext _parentContext;
        private IImmutableMap<string, IActorRef> _hubs = ImmutableTreeMap<string, IActorRef>.Empty; 

        private readonly Serialization.Serializer _serializer =
            Context.System.Serialization.FindSerializerForType(typeof(object));

        public HubConnection(IUntypedActorContext parentContext)
        {
            _parentContext = parentContext;
            
            Receive<Tcp.Received>(received =>
            {
                var message = _serializer.FromBinary(received.Data.ToArray(), typeof (object));
                Self.Forward(message);
            });

            Receive<Hub.Join>(join =>
            {
                //If client is already connected to this hub
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
                //If client already left this hub
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

            Receive<Close>(_ =>
            {
                foreach (var hubPair in _hubs.AllMinToMax)
                {
                    var name = hubPair.Key;
                    var hub = hubPair.Value;

                    hub.Forward(new Hub.Leave(name));
                }

                Context.Stop(Self);
            });
        }

        private Tcp.Write WriteObject(object value) => Tcp.Write.Create(ByteString.Create(_serializer.ToBinary(value)));

        internal class Close
        {
            public static readonly Close Instance = new Close();

            private Close() { }
        }
    }
}
