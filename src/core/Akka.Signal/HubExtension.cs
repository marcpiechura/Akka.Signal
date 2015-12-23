using Akka.Actor;

namespace Akka.Signal
{
    public static class HubExtension
    {
        private static IActorRef _hub = Nobody.Instance;

        public static IActorRef SignalHub(this ActorSystem system)
        {
            if (Nobody.Instance.Equals(_hub))
                _hub = system.ActorOf<HubManager>(HubManager.Name);

            return _hub;
        }
    }
}