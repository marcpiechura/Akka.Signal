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
        public void Bind_to_any_address_and_given_port_on_start()
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

            hub.Tell(new HubManager.StartHub("hub"));
            Task.Delay(500).Wait();

            var result = Sys.ActorSelection("user/AkkaSignalHub/hub").ResolveOne(TimeSpan.FromSeconds(3)).Result;
        }

        [Test]
        public void Do_not_start_a_hub_twice()
        {
            var hub = ActorOf(() => new HubManager(TestEndPoint, TestActor), "AkkaSignalHub");

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

            hub.Tell(new HubManager.StartHub("hub"), TestActor);

            var msg = ExpectMsg<Hub.Registered>();
            Assert.AreEqual("hub", msg.Hub.Path.Name);
        }
    }
}