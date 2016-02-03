using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Signal;

namespace Client
{
    static class Program
    {
        static void Main(string[] args)
        {
            using (var sys = ActorSystem.Create("Signal"))
            {
                var myclient = sys.ActorOf(Props.Create(() => new MyClient()));
                sys.SignalClient().Tell(new Signal.Bind(new DnsEndPoint("localhost", 5678)));
                sys.SignalClient().Tell(new Signal.RegisterClient("Test"), myclient);
                sys.WhenTerminated.Wait();
            }
        }
    }
    
    public class MyClient : ClientActor
    {
        protected override void OnLog(string message) => Console.WriteLine(message);
        protected override void OnJoined(string clientId, string hubName) => Console.WriteLine($"Joined {hubName} as id {clientId}");
        protected override void OnReceived(object message)
        {
            Console.WriteLine(message);
            Send($"ack from client: {Id}, length: {message?.ToString()?.Length ?? 0}");
        }
    }
}
