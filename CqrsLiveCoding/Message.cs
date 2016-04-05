using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new List<object>();
            eventsStore.Add(new MessageQuacked("Hello"));
            eventsStore.Add(new MessageDeleted());
            var message = new Message(eventsStore);

            message.Delete(eventsStore);

            Check.That(eventsStore.OfType<MessageDeleted>()).HasSize(1);
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
        private bool _isDeleted = false;

        public Message(IEnumerable<object> history)
        {
            foreach (var evt in history)
            {
                if (evt is MessageDeleted)
                {
                    Apply((MessageDeleted)evt);
                }
            }
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static Message Quack(List<object> eventsStore, string content)
        {
            var evt = new MessageQuacked(content);
            eventsStore.Add(evt);

            return new Message(new object[]{evt});
        }

        public void Delete(List<object> eventsStore)
        {
            if (_isDeleted) return;

            eventsStore.Add(new MessageDeleted());
        }
    }
}
