using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.IO;

namespace Akka.Signal
{
    public static class MessageSeriliazer
    {
        public static ByteString Seriliaze(ActorSystem system, object message)
        {
            var seriliazer = system.Serialization.FindSerializerFor(message);
            var seriliazerIdBytes = ByteString.FromString(seriliazer.Identifier.ToString());
            var result = ByteString.Create(new[] {Convert.ToByte(seriliazerIdBytes.Count)});
            result = result.Concat(seriliazerIdBytes.Concat(ByteString.Create(seriliazer.ToBinary(message))));

            return result.Concat(ByteString.Create(new[] {Byte.MinValue}));
        }

        public static List<object> Deseriliaze(ActorSystem system, ByteString message)
        {
            var result = new List<object>();

            while (message.Contains(Byte.MinValue) && message.Count > 1)
            {
                var splittedMessage = message.SplitAt(message.IndexOf(Byte.MinValue));
                var currentMessage = splittedMessage.Item1;
                message = splittedMessage.Item2.Drop(1);

                var seriliazerIdLength = Convert.ToInt32(currentMessage.Head);
                var seriliazerId = Int32.Parse(currentMessage.Drop(1).Take(seriliazerIdLength).DecodeString());
                var seriliazer = system.Serialization.GetSerializerById(seriliazerId);
                currentMessage = currentMessage.Drop(1 + seriliazerIdLength);
                result.Add(seriliazer.FromBinary(currentMessage.ToArray(), typeof(object)));
            }

            return result;
        }
    }
}
