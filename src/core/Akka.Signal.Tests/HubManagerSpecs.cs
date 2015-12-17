using System;
using System.Net;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.IO;
using NUnit.Framework;

namespace Akka.Signal.Tests
{
    [TestFixture]
    class HubManagerSpecs : TestKit.NUnit.TestKit
    {
        private static readonly IPEndPoint TestEndPoint = new IPEndPoint(IPAddress.Any, 1234);

        [Test]
        public void Bind_to_given_endpoint_on_start()
        {
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor));

            var message = ExpectMsg<Tcp.Bind>();

            Assert.AreEqual(message.Handler, hub);
            Assert.AreEqual(message.LocalAddress as IPEndPoint, TestEndPoint);
        }

        [Test]
        public void Stop_if_could_not_bound_to_endpoint()
        {
            IgnoreMessages(o => o is Tcp.Bind);
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor));
            Watch(hub);

            hub.Tell(new Tcp.CommandFailed(new Tcp.Bind(hub, TestEndPoint)));

            ExpectTerminated(hub);
        }

        [Test]
        public void Start_a_new_hub()
        {
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");
            hub.Tell(new Tcp.Bound(null));

            hub.Tell(new HubManager.StartHub("hub"));
            Task.Delay(500).Wait();

            var result = Sys.ActorSelection("user/AkkaSignalHub/hub").ResolveOne(TimeSpan.FromSeconds(3)).Result;
        }

        [Test]
        public void Do_not_start_a_hub_twice()
        {
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");
            hub.Tell(new Tcp.Bound(null));

            hub.Tell(new HubManager.StartHub("hub"));
            hub.Tell(new HubManager.StartHub("hub"));
            Task.Delay(500).Wait();

            var result = Sys.ActorSelection("user/AkkaSignalHub/hub").ResolveOne(TimeSpan.FromSeconds(3)).Result;
        }

        [Test]
        public void Automatically_register_the_sender_of_start_hub_on_the_hub()
        {
            IgnoreMessages(o => !(o is Hub.Registered));
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");
            hub.Tell(new Tcp.Bound(null));

            hub.Tell(new HubManager.StartHub("hub"), TestActor);

            var msg = ExpectMsg<Hub.Registered>();
            Assert.AreEqual("hub", msg.Hub.Path.Name);
        }

        [Test]
        public void Create_a_new_hub_connection_when_a_client_connect()
        {
            IgnoreMessages(o => o is Tcp.Bind);
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");
            hub.Tell(new Tcp.Bound(null));

            hub.Tell(new Tcp.Connected(null, null), TestActor);

            var msg = ExpectMsg<Tcp.Register>();
            Assert.AreEqual(hub.Path, msg.Handler.Path.Parent);
            Assert.AreEqual($"~connection_{TestActor.Path.Name}", msg.Handler.Path.Name);
            var result = Sys.ActorSelection($"user/AkkaSignalHub/~connection_{TestActor.Path.Name}").ResolveOne(TimeSpan.FromSeconds(3)).Result;
        }

        [Test]
        public void Stop_hub_connection_on_closed_connection()
        {
            IgnoreMessages(o => !(o is Terminated));
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");
            hub.Tell(new Tcp.Bound(null));
            hub.Tell(new Tcp.Connected(null, null), TestActor);
            Task.Delay(500).Wait();
            var con = Sys.ActorSelection($"user/AkkaSignalHub/~connection_{TestActor.Path.Name}").ResolveOne(TimeSpan.FromSeconds(1)).Result;
            Watch(con);

            hub.Tell(new Tcp.ConnectionClosed(), TestActor);

            ExpectTerminated(con);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(InvalidActorNameException), ExpectedMessage = "The name of a hub MUST not start with '~connection'.")]
        public void Do_not_allow_connection_name_as_hub_name() => new HubManager.StartHub("~connection_hub");
    }
}