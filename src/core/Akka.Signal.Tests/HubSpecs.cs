using System.Linq;
using Akka.Actor;
using Akka.IO;
using NUnit.Framework;

namespace Akka.Signal.Tests
{
    [TestFixture]
    class HubSpecs : TestKit.NUnit.TestKit
    {
        [Test]
        public void Reply_to_sender_with_registered_on_register_message()
        {
            var hub = ActorOf(() => new Hub(TestActor));

            hub.Tell(new Hub.Register(), TestActor);

            var msg = ExpectMsg<Hub.Registered>();
            Assert.AreEqual(hub, msg.Hub);
        }

        [Test]
        public void Add_a_connected_client_to_the_hub()
        {
            var hub = ActorOfAsTestActorRef<Hub>(TestActor);

            hub.Tell(new Hub.Join(TestActor));

            Assert.AreEqual(TestActor, hub.UnderlyingActor.Clients.First());
        }

        [Test]
        public void Send_a_connected_message_back_to_the_client_when_connected()
        {
            var hub = ActorOf(Props.Create(()=> new Hub(TestActor)) ,"hub");

            hub.Tell(new Hub.Join(TestActor));

            var msg = ExpectMsg<Tcp.Write>();
        }
    }
}
