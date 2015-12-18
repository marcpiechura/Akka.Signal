using System;
using Akka.Actor;
using Akka.Signal;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var sys = ActorSystem.Create("Server"))
            {
                var hub = sys.Hub(5678);
                var producer = sys.ActorOf(Props.Create(() => new Producer()));

                hub.Tell(new HubManager.StartHub("Test"), producer);

                sys.AwaitTermination();
            }
        }
    }


    class Producer : ReceiveActor
    {
        private int _counter;
        private IActorRef _hub;

        public Producer()
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(1000, 1000, Self, "", Nobody.Instance);

            Receive<HubManager.HubStarted>(started => _hub = started.Hub);
            Receive<string>(_ => _hub.Tell(DateTime.Now.ToLongTimeString()));
        }
    }
}
