using Akka.Actor;

namespace Akka.Signal
{
    public static class ClientExtension
    {
        private static IActorRef _clientManager = Nobody.Instance;

        public static IActorRef SignalClient(this ActorSystem system)
        {
            if (Equals(_clientManager, Nobody.Instance))
                _clientManager = system.ActorOf<ClientManager>(ClientManager.Name);

            return _clientManager;
        }
    }
}
