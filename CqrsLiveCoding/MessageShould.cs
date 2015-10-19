using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var eventsStore = new List<object>();

            var id = Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked(id, "Hello"));
        }
    }

    public class Message
    {
        public static string Quack(List<object> eventsStore, string message)
        {
            var id = Guid.NewGuid().ToString();
            eventsStore.Add(new MessageQuacked(id, message));

            return id;
        }
    }

    public struct MessageQuacked
    {
        public string Id { get; private set; }
        public string Message { get; private set; }

        public MessageQuacked(string id, string message)
        {
            Id = id;
            Message = message;
        }
    }
}
