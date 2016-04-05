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

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new List<object>();
            var message = Message.Quack(eventsStore, "Hello");

            message.Delete(eventsStore);

            Check.That(eventsStore).Contains(new MessageDeleted());
        }
    }

    public struct MessageDeleted
    {
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
        public Message(IEnumerable<object> history)
        {
        }

        public static Message Quack(List<object> eventsStore, string content)
        {
            var evt = new MessageQuacked(content);
            eventsStore.Add(evt);

            return new Message(new object[]{evt});
        }

        public void Delete(List<object> eventsStore)
        {
            eventsStore.Add(new MessageDeleted());
        }
    }
}
