using System.Collections;
using System.Collections.Generic;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackWhenQuackMessage()
        {
            var eventsStore = new List<object>();

            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked("Hello"));
        }
    }

    public struct MessageQuacked
    {
        public string Content { get; private set; }

        public MessageQuacked(string content)
        {
            Content = content;
        }
    }

    public class Message
    {
        public static void Quack(List<object> eventsStore, string content)
        {
            eventsStore.Add(new MessageQuacked(content));
        }
    }
}
