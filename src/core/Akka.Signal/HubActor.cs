using Akka.Actor;

namespace Akka.Signal
{
    public abstract class HubActor : ReceiveActor
    {
        private IActorRef _hub;
        private string _hubName;

        public HubActor()
        {
            Receive<Signal.HubStarted>(started =>
            {
                _hub = started.Hub;
                _hubName = started.Name;
                Become(Started);
            });
        }

        private void Started()
        {
            Receive<Signal.Joined>(x => OnClientJoined(x.Client));
            Receive<Signal.Left>(x => OnClientLeft(x.Client));
            Receive<Signal.ClientBroadcast>(x => OnReceived(x.Client, x.Message));
            OnStarted();
        }

        protected virtual void OnStarted() { }
        protected virtual void OnClientJoined(string clientId) { }
        protected virtual void OnClientLeft(string clientId) { }
        protected virtual void OnReceived(string clientId, object message) { }

        protected void Send(object message, string clientId = null)
        {
            _hub.Tell(new Signal.Broadcast(_hubName, message, clientId));
        }
    }
}
