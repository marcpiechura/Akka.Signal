using System;
using Akka.Actor;
using Akka.IO;
using Akka.Signal;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sys = ActorSystem.Create("Server"))
            {
                sys.SignalHub().Tell(new Signal.Bind(5678));

                var producer = sys.ActorOf(Props.Create(() => new Producer()));
                sys.SignalHub().Tell(new Signal.StartHub("Test"), producer);

                sys.WhenTerminated.Wait();
            }
        }
    }


    class Producer : ReceiveActor
    {
        private IActorRef _hub;

        public Producer()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(1000, 1000, Self, "", Nobody.Instance);

            Receive<Signal.HubStarted>(started => _hub = started.Hub);
            Receive<string>(_ => _hub.Tell(DateTime.Now.ToLongTimeString()));
        }
    }
}
