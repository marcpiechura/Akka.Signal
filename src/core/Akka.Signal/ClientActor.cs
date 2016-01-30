using Akka.Actor;

namespace Akka.Signal
{
    public abstract class ClientActor : ReceiveActor
    {
        private string _ownId;
        private IActorRef _client;
        private string _hub;

        protected string Id => _ownId;

        public ClientActor()
        {
            Receive<Signal.Connecting>(connecting => log("Connecting to server..."));
            Receive<Signal.Connected>(connected => log("Successfully connected"));
            Receive<Signal.Disconnected>(disconnected =>
            {
                _ownId = string.Empty;
                log("Connection to the server got lost...");
            });
            Receive<Signal.Reconnecting>(reconnecting => log("Reconnecting to server..."));
            Receive<Signal.Reconnected>(reconnected => log("Successfully reconnected to the server"));
            Receive<Signal.ClientRegistered>(registered =>
            {
                _client = registered.Client;
                _hub = registered.Hub;
                log($"Client successfully registered for hub {_hub} on connection {_client}");
            });
            Receive<Signal.Joined>(
                joined =>
                {
                    if (string.IsNullOrWhiteSpace(_ownId))
                    {
                        _ownId = joined.Client;
                        log($"Successfully joined {joined.HubName} with clientId {_ownId}, we now receive updates from this hub");
                        OnJoined(_ownId, joined.HubName);
                    }
                    else
                        log($"A new client has joined the hub {joined.HubName} with clientId {joined.Client}");
                });
            Receive<Signal.Left>(left => log($"The client {left.Client} hast left the hub {left.Hub}"));
            ReceiveAny(OnReceived);
        }

        private void log(string msg) => OnLog(msg);

        protected virtual void OnLog(string message) { }
        protected virtual void OnJoined(string clientId, string hubName) { }
        protected virtual void OnReceived(object message) { }

        protected void Send(object message) => _client?.Tell(message);
    }
}
