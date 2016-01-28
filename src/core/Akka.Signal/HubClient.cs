using System;
using System.Net;
using Akka.Actor;
using Akka.IO;

namespace Akka.Signal
{
    public class HubClient : ReceiveActor
    {
        private readonly EndPoint _endPoint;
        private readonly IActorRef _handler;
        private readonly string _hub;

        public HubClient(EndPoint endPoint, IActorRef handler, string hub)
        {
            _endPoint = endPoint;
            _handler = handler;
            _hub = hub;
            Become(Connecting());
        }

        private Receive Connecting()
        {
            Connect(_endPoint);

            return message =>
            {
                var connected = message as Tcp.Connected;
                if (connected != null)
                {
                    _handler.Tell(new Signal.Connected());
                    Become(Connected(Sender));
                    return true;
                }

                var failed = message as Tcp.CommandFailed;
                if (failed?.Cmd is Tcp.Connect)
                {
                    Connect(_endPoint);
                    return true;
                }

                return false;
            };
        }

        private Receive Connected(IActorRef connection)
        {
            connection.Tell(new Tcp.Register(Self));
            connection.Tell(Tcp.Write.Create(MessageSeriliazer.Serialize(Context.System, new Signal.Join(_hub))));

            return message =>
            {
                var received = message as Tcp.Received;
                if (received != null)
                {
                    var messages = MessageSeriliazer.Deserialize(Context.System, received.Data);
                    messages.ForEach(_handler.Tell);
                    return true;
                }
                
                var closed = message as Tcp.ConnectionClosed;
                if (closed != null)
                {
                    _handler.Tell(new Signal.Disconnected());
                    Become(Reconnecting());
                    return true;
                }

                return false;
            };
        }

        private Receive Reconnecting()
        {
            Connect(_endPoint, true);

            return message =>
            {
                var connected = message as Tcp.Connected;
                if (connected != null)
                {
                    _handler.Tell(new Signal.Reconnected());
                    Become(Connected(Sender));
                    return true;
                }

                var failed = message as Tcp.CommandFailed;
                if (failed?.Cmd is Tcp.Connect)
                {
                    Connect(_endPoint, true);
                    return true;
                }

                return true;
            };
        }
        


        private void Connect(EndPoint endPoint, bool reconnecting = false)
        {
            Context.System.Tcp().Tell(new Tcp.Connect(endPoint, null, null, TimeSpan.FromSeconds(3)));

            object message = new Signal.Connecting();
            if(reconnecting)
                message = new Signal.Reconnecting();

            _handler.Tell(message);
        }
    }
}