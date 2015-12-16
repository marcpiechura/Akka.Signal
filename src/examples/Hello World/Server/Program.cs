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
                var producer = sys.ActorOf(Props.Create(() => new Producer(hub)));

                sys.AwaitTermination();
            }
        }
    }


    class Producer : ReceiveActor
    {
        private int _counter;

        public Producer(IActorRef hub)
        {
            Context.System.Scheduler.ScheduleTellRepeatedly(1000, 1000, Self, "", Nobody.Instance);

            //ReceiveAny(_ => hub.Tell(new Hub.Broadcast(++_counter)));
        }
    }
}
