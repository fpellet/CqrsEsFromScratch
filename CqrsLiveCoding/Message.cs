using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        private readonly EventsStreamFake _eventsStream = new EventsStreamFake();

        [Fact]
        public void RaiseMessageQuackWhenQuackMessage()
        {
            Message.Quack(_eventsStream, "Hello");

            Check.That(_eventsStream.Historic).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsStream);

            Check.That(_eventsStream.Historic).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            _eventsStream.Add(new MessageDeleted());
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsStream);

            Check.That(_eventsStream.Historic.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsStream);
            message.Delete(_eventsStream);

            Check.That(_eventsStream.Historic.OfType<MessageDeleted>()).HasSize(1);
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

    public interface IEventsStream
    {
        void Add(IDomainEvent evt);
    }

    public class EventsStreamFake : IEventsStream
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

        public static void Quack(IEventsStream history, string content)
        {
            history.Add(new MessageQuacked(content));
        }

        public void Delete(IEventsStream history)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            history.Add(evt);
            Apply(evt);
        }
    }
}
