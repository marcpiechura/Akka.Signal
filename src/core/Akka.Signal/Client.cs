using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Akka.Actor;
using Akka.IO;

namespace Akka.Signal
{
    public class HubClient : ReceiveActor
    {
        private readonly HashSet<IActorRef> _reciepients = new HashSet<IActorRef>();
        private readonly EndPoint _endPoint;

        public HubClient(string host, int port)
        {
            _endPoint = new DnsEndPoint(host, port);
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
                    TellRecipients(new Connected());
                    Become(Connected(Sender));
                    return true;
                }

                var failed = message as Tcp.CommandFailed;
                if (failed?.Cmd is Tcp.Connect)
                {
                    Connect(_endPoint);
                    return true;
                }

                var register = message as Register;
                if (register != null)
                {
                    _reciepients.Add(register.Reciepient);
                    return true;
                }

                return false;
            };
        }

        private Receive Connected(IActorRef connection)
        {
            connection.Tell(new Tcp.Register(Self));
            connection.Tell(Tcp.Write.Create(MessageSeriliazer.Seriliaze(Context.System, new Hub.Join("Test"))));

            return message =>
            {
                var received = message as Tcp.Received;
                if (received != null)
                {
                    var messages = MessageSeriliazer.Deseriliaze(Context.System, received.Data);
                    messages.ForEach(TellRecipients);
                    return true;
                }
                
                var closed = message as Tcp.ConnectionClosed;
                if (closed != null)
                {
                    TellRecipients(new Disconnected());
                    Become(Reconnecting());
                    return true;
                }

                var register = message as Register;
                if (register != null)
                {
                    _reciepients.Add(register.Reciepient);
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
                    TellRecipients(new Reconnected());
                    Become(Connected(Sender));
                    return true;
                }

                var failed = message as Tcp.CommandFailed;
                if (failed?.Cmd is Tcp.Connect)
                {
                    Connect(_endPoint, true);
                    return true;
                }

                var register = message as Register;
                if (register != null)
                {
                    _reciepients.Add(register.Reciepient);
                    return true;
                }

                return true;
            };
        }
        


        private void Connect(EndPoint endPoint, bool reconnecting = false)
        {
            Context.System.Tcp().Tell(new Tcp.Connect(endPoint, null, null, TimeSpan.FromSeconds(3)));

            object message = new Connecting();
            if(reconnecting)
                message = new Reconnecting();

            TellRecipients(message);
        }

        private void TellRecipients(object message)
        {
            foreach (var reciepient in _reciepients.AsParallel())
                reciepient.Tell(message);
        }
    }

    public class Connected
    {
    }

    public class Reconnected
    {
    }

    public class Disconnected
    {
    }

    public class Connecting
    {
    }

    public class Reconnecting 
    {
    }

    public class Register
    {
        public Register(IActorRef reciepient)
        {
            Reciepient = reciepient;
        }

        public IActorRef Reciepient { get; private set; }
    }
}
