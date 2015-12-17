﻿using System;
using System.Net;
using Akka.Actor;
using Akka.Event;
using Akka.IO;

namespace Akka.Signal
{
    public sealed class HubManager : ReceiveActor, IWithUnboundedStash
    {
        public const string Name = "AkkaSignalHub";
        private const string ConnecteionPrefix = "~connection";

        private readonly EndPoint _endPoint;
        private readonly IActorRef _tcpManager;
        private readonly ILoggingAdapter _log = Context.GetLogger();

        private static readonly Func<IActorRef, string> GetConnectionNameFromActor = actor => $"{ConnecteionPrefix}_{actor.Path.Name}";

        public IStash Stash { get; set; }

        public HubManager(int port) : this(new IPEndPoint(IPAddress.Any, port))
        {
        }

        public HubManager(EndPoint endPoint) : this(endPoint, Context.System.Tcp())
        {
        }

        public HubManager(EndPoint endPoint, IActorRef tcpManager)
        {
            _endPoint = endPoint;
            _tcpManager = tcpManager;

            Become(Binding);
        }

        private void Binding()
        {
            Receive<Tcp.CommandFailed>(failed => failed.Cmd is Tcp.Bind, failed =>
            {
                _log.Error($"Could not bind to endpoint {_endPoint}. Reason: {failed.Cmd.FailureMessage}");
                Context.Stop(Self);
            });

            Receive<Tcp.Bound>(bound =>
            {
                _log.Info($"Listening on {bound.LocalAddress}");
                Become(Bound);
            });

            ReceiveAny(o => Stash.Stash());

            _tcpManager.Tell(new Tcp.Bind(Self, _endPoint));
        }

        private void Bound()
        {
            Receive<StartHub>(hub =>
            {
                var child = Context.Child(hub.HubName);

                if (child.IsNobody())
                {
                    _log.Info($"New hub {hub.HubName} created");
                    child = Context.ActorOf(Props.Create(() => new Hub()), hub.HubName);
                }

                child.Tell(new Hub.Register(), Sender);
            });

            Receive<Tcp.Connected>(connected =>
            {
                _log.Info($"New client connection {Sender.Path.Name} established. Remote: {connected.RemoteAddress} -> Local: {connected.LocalAddress}");

                var hubConncetion = Context.ActorOf(Props.Create(() => new HubConnection(Context)),
                    GetConnectionNameFromActor(Sender));
                Sender.Tell(new Tcp.Register(hubConncetion));
            });

            Receive<Tcp.ConnectionClosed>(closed =>
            {
                _log.Info($"Client connection {Sender.Path.Name} was closed. Reason: {closed.GetErrorCause()}");

                var con = Context.Child(GetConnectionNameFromActor(Sender));
                if (!con.Equals(Nobody.Instance))
                    Context.Stop(con);
            });

            Receive<Tcp.Write>(write =>
            {
                var hubConnection = Context.Child(GetConnectionNameFromActor(Sender));
                if (hubConnection.IsNobody())
                    return;
                
                hubConnection.Forward(write);
            });

            Stash.UnstashAll();
        }

        public class StartHub
        {
            public StartHub(string hubName)
            {
                if(hubName.StartsWith(ConnecteionPrefix))
                    throw new InvalidActorNameException("The name of a hub MUST not start with '~connection'.");

                HubName = hubName;
            }

            public string HubName { get; private set; }
        }

    }
}
