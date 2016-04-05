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
            var eventsStore = new List<IDomainEvent>();

            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new List<IDomainEvent>();
            var message = Message.Quack(eventsStore, "Hello");

            message.Delete(eventsStore);

            Check.That(eventsStore).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new List<IDomainEvent>();
            eventsStore.Add(new MessageQuacked("Hello"));
            eventsStore.Add(new MessageDeleted());
            var message = new Message(eventsStore);

            message.Delete(eventsStore);

            Check.That(eventsStore.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            var eventsStore = new List<IDomainEvent>();
            eventsStore.Add(new MessageQuacked("Hello"));
            var message = new Message(eventsStore);

            message.Delete(eventsStore);
            message.Delete(eventsStore);

            Check.That(eventsStore.OfType<MessageDeleted>()).HasSize(1);
        }
    }

    public interface IDomainEvent
    {
    }

    public struct MessageDeleted : IDomainEvent
    {
    }

    public struct MessageQuacked : IDomainEvent
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

        public Message(IEnumerable<IDomainEvent> history)
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

        public static Message Quack(List<IDomainEvent> eventsStore, string content)
        {
            var evt = new MessageQuacked(content);
            eventsStore.Add(evt);

            return new Message(new IDomainEvent[]{evt});
        }

        public void Delete(List<IDomainEvent> eventsStore)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            eventsStore.Add(evt);
            Apply(evt);
        }
    }
}
