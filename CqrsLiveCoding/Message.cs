using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class EventsBusShould
    {
        [Fact]
        public void StoreEventsWhenPublishEvent()
        {
            var eventsStream = new EventsStreamFake();
            var eventsPublisher = new EventsBus(eventsStream);
            var evt = new MessageQuacked("Hello");

            eventsPublisher.Publish(evt);

            Check.That(eventsStream.History).ContainsExactly(evt);
        }

        [Fact]
        public void CallHandlersWhenPublishEvent()
        {
            var handler1 = new EventHandlerFake<MessageQuacked>();
            var handler2 = new EventHandlerFake<MessageQuacked>();
            var handler3 = new EventHandlerFake<MessageDeleted>();
            var eventsPublisher = new EventsBus(new EventsStreamFake());
            var evt = new MessageQuacked("Hello");
            eventsPublisher.Subscribe(handler1);
            eventsPublisher.Subscribe(handler2);
            eventsPublisher.Subscribe(handler3);
            
            eventsPublisher.Publish(evt);

            Check.That(handler1.Event).IsEqualTo(evt);
            Check.That(handler2.Event).IsEqualTo(evt);
            Check.That(handler3.Event).IsNull();
        }

        private class EventHandlerFake<TEvent> : IDomainEventHandler<TEvent> 
            where TEvent : IDomainEvent
        {
            public IDomainEvent Event { get; private set; }
            
            public void When(TEvent evt)
            {
                Event = evt;
            }
        }
    }

    public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
        where TEvent: IDomainEvent
    {
        void When(TEvent evt);
    }
    
    public interface IEventsPublisher
    {
        void Publish<TEvent>(TEvent evt)
            where TEvent : IDomainEvent;
    }

    public interface IDomainEventHandler
    {
    }

    public class EventsBus : IEventsPublisher
    {
        private readonly IEventsStream _eventsStream;
        private readonly IList<IDomainEventHandler> _handlers = new List<IDomainEventHandler>();

        public EventsBus(IEventsStream eventsStream)
        {
            _eventsStream = eventsStream;
        }

        public void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent
        {
            _eventsStream.Add(evt);

            foreach (var handler in _handlers.OfType<IDomainEventHandler<TEvent>>())
            {
                handler.When(evt);
            }
        }

        public void Subscribe(IDomainEventHandler handler)
        {
            _handlers.Add(handler);
        }
    }

    public class TimelineShould
    {
        [Fact]
        public void DisplayMessageWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.When(new MessageQuacked("Hello"));

            Check.That(timeline.Messages).ContainsExactly(new TimelineMessage("Hello"));
        }
    }

    public class Timeline
    {
        public IList<TimelineMessage> Messages { get; } = new List<TimelineMessage>();
        
        public void When(MessageQuacked evt)
        {
            Messages.Add(new TimelineMessage(evt.Message));
        }
    }

    public struct TimelineMessage
    {
        public string Content { get; }

        public TimelineMessage(string content)
        {
            Content = content;
        }
    }

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
        private readonly EventsStreamFake _eventsStream;
        private readonly EventsBus _eventsBus;

        public MessageShould()
        {
            _eventsStream = new EventsStreamFake();
            _eventsBus = new EventsBus(_eventsStream);
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            Message.Quack(_eventsBus, "Hello");

            Check.That(_eventsStream.History).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.History);

            message.Delete(_eventsBus);

            Check.That(_eventsStream.History).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseDeletedWhenDeleteDeletedMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            _eventsStream.Add(new MessageDeleted());
            var message = new Message(_eventsStream.History);
            
            message.Delete(_eventsBus);

            Check.That(_eventsStream.History.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.History);
            
            message.Delete(_eventsBus);
            message.Delete(_eventsBus);

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

        public static void Quack(IEventsPublisher eventsPublisher, string message)
        {
            eventsPublisher.Publish(new MessageQuacked(message));
        }

        public void Delete(IEventsPublisher eventsPublisher)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            eventsPublisher.Publish(evt);
            Apply(evt);
        }
    }
}
