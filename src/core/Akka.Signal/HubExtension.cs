using System.Net;
using Akka.Actor;

namespace Akka.Signal
{
    public static class HubExtension
    {
        private static IActorRef _hub = Nobody.Instance;

        public static IActorRef Hub(this ActorSystem system, int port)
            => Hub(system, new IPEndPoint(IPAddress.Any, port));

        public static IActorRef Hub(this ActorSystem system, EndPoint endPoint)
        {
            if (Nobody.Instance.Equals(_hub))
                _hub = system.ActorOf(Props.Create(() => new HubManager(endPoint)), HubManager.Name);

            return _hub;
        }
    }
}