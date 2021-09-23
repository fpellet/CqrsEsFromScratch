using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class QuackCounterShould
    {
        [Fact]
        public void IncrementWhenMessageQuacked()
        {
            var counter = new QuackCounter();

            counter.When(new MessageQuacked("Hello"));

            Check.That(counter.QuacksNb).IsEqualTo(1);
        }
        
        [Fact]
        public void DecrementWhenMessageDeleted()
        {
            var counter = new QuackCounter();
            counter.When(new MessageQuacked("Hello"));

            counter.When(new MessageDeleted());

            Check.That(counter.QuacksNb).IsEqualTo(0);
        }
    }

    public class QuackCounter
    {
        public int QuacksNb { get; private set; }
        
        public void When(MessageQuacked evt)
        {
            QuacksNb++;
        }

        public void When(MessageDeleted evt)
        {
            QuacksNb--;
        }
    }

    public class MessageShould
    {
        private readonly EventsStreamFake _eventsStream = new EventsStreamFake();

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            Message.Quack(_eventsStream, "Hello");

            Check.That(_eventsStream.History).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.History);

            message.Delete(_eventsStream);

            Check.That(_eventsStream.History).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseDeletedWhenDeleteDeletedMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            _eventsStream.Add(new MessageDeleted());
            var message = new Message(_eventsStream.History);
            
            message.Delete(_eventsStream);

            Check.That(_eventsStream.History.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.History);
            
            message.Delete(_eventsStream);
            message.Delete(_eventsStream);

            Check.That(_eventsStream.History.OfType<MessageDeleted>()).HasSize(1);
        }
    }

    public struct MessageDeleted : IDomainEvent
    {
    }

    public interface IDomainEvent
    {
    }

    public interface IEventsStream
    {
        void Add(IDomainEvent evt);
    }
    
    public class EventsStreamFake : IEventsStream
    {
        public List<IDomainEvent> History { get; } = new List<IDomainEvent>();
        
        public void Add(IDomainEvent evt)
        {
            History.Add(evt);
        }
    }

    public struct MessageQuacked : IDomainEvent
    {
        public string Message { get; private set; }

        public MessageQuacked(string message)
        {
            Message = message;
        }
    }

    public class Message
    {
        private bool _isDeleted = false;

        public Message(List<IDomainEvent> history)
        {
            foreach (var evt in history)
            {
                if (evt is MessageDeleted deleted)
                {
                    Apply(deleted);
                }
            }
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static void Quack(IEventsStream eventsStream, string message)
        {
            eventsStream.Add(new MessageQuacked(message));
        }

        public void Delete(IEventsStream eventsStream)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            eventsStream.Add(evt);
            Apply(evt);
        }
    }
}
