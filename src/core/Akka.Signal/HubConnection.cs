using Akka.Actor;
using Akka.IO;
using Akka.Util.Internal.Collections;

namespace Akka.Signal
{
    public class HubConnection : ReceiveActor
    {
        private readonly IUntypedActorContext _parentContext;
        private ImmutableTreeMap<IActorRef, IActorRef> _clientHubMap = ImmutableTreeMap<IActorRef, IActorRef>.Empty; 

        private readonly Serialization.Serializer _serializer =
            Context.System.Serialization.FindSerializerForType(typeof(object));

        public HubConnection(IUntypedActorContext parentContext)
        {
            _parentContext = parentContext;

            Receive<Tcp.Write>(write =>
            {
                var message = _serializer.FromBinary(write.Data.ToArray(), typeof (object));
                Self.Forward(message);
            });

            Receive<Hub.Connect>(connect =>
            {
                //If client is already connected to a hub
                if (_clientHubMap.Contains(Sender))
                    return;

                var hub = _parentContext.Child(connect.HubName);
                if (hub.IsNobody())
                {
                    Sender.Tell(WriteObject(new Hub.NotFound(connect.HubName)));
                    return;
                }

                hub.Forward(connect);
                _clientHubMap.TryAdd(Sender, hub, out _clientHubMap);
            });
        }


        private Tcp.Write WriteObject(object value) => Tcp.Write.Create(ByteString.Create(_serializer.ToBinary(value)));
    }
}
