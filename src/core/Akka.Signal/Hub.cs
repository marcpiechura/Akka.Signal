using System.Collections.Generic;
using Akka.Actor;
using Akka.Util.Internal.Collections;

namespace Akka.Signal
{
    public class Hub : ReceiveActor
    {
        private ImmutableTreeSet<IActorRef> _clients = ImmutableTreeSet<IActorRef>.Empty;

        public IEnumerable<IActorRef> Clients => _clients;

        public Hub()
        {
            Receive<Register>(register => Sender.Tell(new Registered(Self)));
            
        }



        public class Registered
        {
            public Registered(IActorRef hub)
            {
                Hub = hub;
            }

            public IActorRef Hub { get; private set; }
        }

        public class Register
        {
        }

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
            public Joined(string hubName)
            {
                HubName = hubName;
            }

            public string HubName { get; private set; }
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

        public class Broadcast
        {
            public Broadcast(string hubName)
            {
                HubName = hubName;
            }

            public string HubName { get; private set; }
        }
    }
}
