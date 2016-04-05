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

            Check.That(eventsStream.Historic).ContainsExactly(evt);
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

    public interface IDomainEventHandler
    {

    }

    public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
        where TEvent : IDomainEvent
    {
        void When(TEvent evt);
    }

    public interface IEventsPublisher
    {
        void Publish<TEvent>(TEvent evt)
            where TEvent : IDomainEvent;
    }

    public class EventsBus : IEventsPublisher
    {
        private readonly IEventsStream _eventsStore;
        private readonly IList<IDomainEventHandler> _handlers = new List<IDomainEventHandler>();

        public EventsBus(IEventsStream eventsStore)
        {
            _eventsStore = eventsStore;
        }

        public void Subscribe(IDomainEventHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Publish<TEvent>(TEvent evt)
            where TEvent : IDomainEvent
        {
            _eventsStore.Add(evt);

            foreach (var handler in _handlers.OfType<IDomainEventHandler<TEvent>>())
            {
                handler.When(evt);
            }
        }
    }

    public class QuackCounterShould
    {
        [Fact]
        public void IncrementWhenMessageQuacked()
        {
            var counter = new QuackCounter();

            counter.When(new MessageQuacked("Hello"));

            Check.That(counter.QuackNb).IsEqualTo(1);
        }

        [Fact]
        public void DecrementWhenMessageDeleted()
        {
            var counter = new QuackCounter();
            counter.When(new MessageQuacked("Hello"));

            counter.When(new MessageDeleted());

            Check.That(counter.QuackNb).IsEqualTo(0);
        }
    }

    public class QuackCounter
    {
        public int QuackNb { get; private set; }

        public void When(MessageQuacked evt)
        {
            QuackNb++;
        }

        public void When(MessageDeleted evt)
        {
            QuackNb--;
        }
    }

    public class TimelineShould
    {
        [Fact]
        public void DisplayMessageWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.When(new MessageQuacked("Hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
        }
    }

    public class Timeline
    {
        public IList<TimelineMessage> Messages { get; } = new List<TimelineMessage>();

        public void When(MessageQuacked evt)
        {
            Messages.Add(new TimelineMessage(evt.Content));
        }
    }

    public struct TimelineMessage
    {
        public string Content { get; private set; }

        public TimelineMessage(string content)
        {
            Content = content;
        }
    }

    public class MessageShould
    {
        private readonly EventsStreamFake _eventsStream;
        private readonly IEventsPublisher _eventsPublisher;

        public MessageShould()
        {
            _eventsStream = new EventsStreamFake();
            _eventsPublisher = new EventsBus(_eventsStream);
        }

        [Fact]
        public void RaiseMessageQuackWhenQuackMessage()
        {
            Message.Quack(_eventsPublisher, "Hello");

            Check.That(_eventsStream.Historic).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsPublisher);

            Check.That(_eventsStream.Historic).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            _eventsStream.Add(new MessageDeleted());
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsPublisher);

            Check.That(_eventsStream.Historic.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            _eventsStream.Add(new MessageQuacked("Hello"));
            var message = new Message(_eventsStream.Historic);

            message.Delete(_eventsPublisher);
            message.Delete(_eventsPublisher);

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
        public string Content { get; }

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

        public static void Quack(IEventsPublisher eventsPublisher, string content)
        {
            eventsPublisher.Publish(new MessageQuacked(content));
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
