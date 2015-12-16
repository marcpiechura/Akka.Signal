using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.IO;

namespace Akka.Signal
{
    public sealed class HubManager : ReceiveActor
    {
        public const string Name = "AkkaSignalHub";

        public HubManager(int port) : this(new IPEndPoint(IPAddress.Any, port))
        {

        }

        public HubManager(EndPoint endPoint) : this(endPoint, Context.System.Tcp())
        {

        }

        public HubManager(EndPoint endPoint, IActorRef tcpManager)
        {
            Receive<Tcp.CommandFailed>(failed => failed.Cmd is Tcp.Bind, failed =>
            {
                Context.GetLogger().Error($"Could not bind to endpoint {endPoint}. Reason: {failed.Cmd.FailureMessage}");
                Context.Stop(Self);
            });

            Receive<StartHub>(hub =>
            {
                var child = Context.Child(hub.HubName);

                if (child.IsNobody())
                    child = Context.ActorOf(Props.Create(() => new Hub()), hub.HubName);
                
                child.Tell(new Hub.Register(), Sender);
            });

            tcpManager.Tell(new Tcp.Bind(Self, endPoint));
        }

        public class StartHub
        {
            public StartHub(string hubName)
            {
                HubName = hubName;
            }

            public string HubName { get; private set; }
        }
    }
}
