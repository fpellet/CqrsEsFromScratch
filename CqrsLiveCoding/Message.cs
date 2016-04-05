using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        private readonly EventsStoreFake _eventsStore = new EventsStoreFake();

        [Fact]
        public void RaiseMessageQuackWhenQuackMessage()
        {
            Message.Quack(_eventsStore, "Hello");

            Check.That(_eventsStore.Historic).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var message = Message.Quack(_eventsStore, "Hello");

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Historic).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            _eventsStore.Add(new MessageQuacked("Hello"));
            _eventsStore.Add(new MessageDeleted());
            var message = new Message(_eventsStore.Historic);

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Historic.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void ApplyEventsOnSelf()
        {
            var message = Message.Quack(_eventsStore, "Hello");
            message.Delete(_eventsStore);

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Historic.OfType<MessageDeleted>()).HasSize(1);
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

    public interface IEventsStore
    {
        void Add(IDomainEvent evt);
    }

    public class EventsStoreFake : IEventsStore
    {
        public IList<IDomainEvent> Historic { get; } = new List<IDomainEvent>();

        public void Add(IDomainEvent evt)
        {
            Historic.Add(evt);
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

        public static Message Quack(IEventsStore eventsStore, string content)
        {
            var evt = new MessageQuacked(content);
            eventsStore.Add(evt);

            return new Message(new IDomainEvent[]{evt});
        }

        public void Delete(IEventsStore eventsStore)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            eventsStore.Add(evt);
            Apply(evt);
        }
    }
}
