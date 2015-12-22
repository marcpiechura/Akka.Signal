using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Akka.Actor;
using Akka.IO;

namespace Akka.Signal
{
    public static class MessageSeriliazer
    {
        private const int SeriliazerIdBytesCount = 4;
        private const int MessageLengthBytesCount = 4;
        private const int CompletePrefixBytesCount = SeriliazerIdBytesCount + MessageLengthBytesCount;

        public static ByteString Serialize(ActorSystem system, object message)
        {
            var seriliazer = system.Serialization.FindSerializerFor(message);
            var messageBytes = ByteString.Create(seriliazer.ToBinary(message));
            var messageLength = ByteString.Create(BitConverter.GetBytes(messageBytes.Count));
            var seriliazerId = ByteString.Create(BitConverter.GetBytes(seriliazer.Identifier));

            return seriliazerId.Concat(messageLength.Concat(messageBytes));
        }

        public static List<object> Deserialize(ActorSystem system, ByteString message)
        {
            var result = new List<object>();

            while (!message.IsEmpty)
            {
                var seriliazerId = BitConverter.ToInt32(message.Take(SeriliazerIdBytesCount).ToArray(), 0);
                var messageLength = BitConverter.ToInt32(message.Skip(SeriliazerIdBytesCount).Take(MessageLengthBytesCount).ToArray(), 0);
                var messageBytes = message.Skip(CompletePrefixBytesCount).Take(messageLength).ToArray();
                var seriliazer = system.Serialization.GetSerializerById(seriliazerId);

                result.Add(seriliazer.FromBinary(messageBytes, typeof(object)));

                message = message.Drop(CompletePrefixBytesCount + messageLength);
            }

            return result;
        }
    }
}
