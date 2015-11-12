using System;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class EventsPublisherShould
    {
        [Fact]
        public void StoreEventsWhenPublishEvent()
        {
            var eventsStoreFake = new EventsStoreFake();
            var eventsPublisher = new EventsPublisher(eventsStoreFake);
            var evt = new MessageQuacked("A", "Hello");

            eventsPublisher.Publish(evt);

            Check.That(eventsStoreFake.Events).ContainsExactly(evt);
        }

        [Fact]
        public void CallHandlersWhenPublishEvent()
        {
            var handler1 = new EventHandlerFake<MessageQuacked>();
            var handler2 = new EventHandlerFake<MessageQuacked>();
            var handler3 = new EventHandlerFake<MessageDeleted>();
            var eventsPublisher = new EventsPublisher(new EventsStoreFake());
            var evt = new MessageQuacked("A", "Hello");
            eventsPublisher.Subscribe(handler1);
            eventsPublisher.Subscribe(handler2);
            eventsPublisher.Subscribe(handler3);

            eventsPublisher.Publish(evt);

            Check.That(handler1.Event).IsEqualTo(evt);
            Check.That(handler2.Event).IsEqualTo(evt);
            Check.That(handler3.Event).IsNull();
        }

        public class EventHandlerFake<TEvent> : IMessageEventHandler<TEvent> 
            where TEvent : IMessageEvent
        {
            public IMessageEvent Event { get; private set; }

            public void Handle(TEvent evt)
            {
                Event = evt;
            }
        }
    }

    public interface IMessageEventHandler
    {
        
    }

    public interface IMessageEventHandler<TEvent> : IMessageEventHandler
        where TEvent : IMessageEvent
    {
        void Handle(TEvent evt);
    }

    public interface IEventsPublisher
    {
        void Subscribe(IMessageEventHandler handler);

        void Publish<TEvent>(TEvent evt)
            where TEvent: IMessageEvent;
    }

    public class EventsPublisher : IEventsPublisher
    {
        private readonly IEventsStore _eventsStore;
        private readonly IList<IMessageEventHandler> _handlers = new List<IMessageEventHandler>();

        public EventsPublisher(IEventsStore eventsStore)
        {
            _eventsStore = eventsStore;
        }

        public void Subscribe(IMessageEventHandler handler)
        {
            _handlers.Add(handler);
        }

        public void Publish<TEvent>(TEvent evt)
            where TEvent: IMessageEvent
        {
            _eventsStore.Add(evt);

            foreach (var handler in _handlers.OfType<IMessageEventHandler<TEvent>>())
            {
                handler.Handle(evt);
            }
        }
    }

    public class QuackCounterShould
    {
        private const string Id = "A";
        private const string Content = "Hello";

        [Fact]
        public void IncrementWhenMessageQuacked()
        {
            var counter = new QuackCounter();
            counter.Handle(new MessageQuacked(Id, Content));

            Check.That(counter.Nb).IsEqualTo(1);
        }
    }

    public class QuackCounter : IMessageEventHandler<MessageQuacked>
    {
        public int Nb { get; private set; }

        public void Handle(MessageQuacked messageQuacked)
        {
            Nb++;
        }
    }

    public class TimelineShould
    {
        [Fact]
        public void AddMessageinTimelineWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.Handle(new MessageQuacked("A", "Hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
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

    public class Timeline : IMessageEventHandler<MessageQuacked>
    {
        public ICollection<TimelineMessage> Messages { get; } = new List<TimelineMessage>();

        public void Handle(MessageQuacked evt)
        {
            Messages.Add(new TimelineMessage(evt.Content));
        }
    }

    public class MessageShould
    {
        private const string Id = "A";
        private const string Content = "Hello";

        private readonly EventsStoreFake _eventsStore;
        private readonly IEventsPublisher _eventsPublisher;

        public MessageShould()
        {
            _eventsStore = new EventsStoreFake();
            _eventsPublisher = new EventsPublisher(_eventsStore);
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var message = Message.Quack(_eventsPublisher, Content);

            Check.That(_eventsStore.Events)
                .ContainsExactly(new MessageQuacked(message.GetId(), Content));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMesage()
        {
            var message = new Message(new MessageQuacked(Id, Content));

            message.Delete(_eventsPublisher);

            Check.That(_eventsStore.Events).ContainsExactly(new MessageDeleted(Id));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var message = new Message(new MessageQuacked(Id, Content), new MessageDeleted(Id));

            message.Delete(_eventsPublisher);

            Check.That(_eventsStore.Events).IsEmpty();
        }
    }

    public interface IEventsStore
    {
        void Add(IMessageEvent evt);
    }

    public class EventsStoreFake : IEventsStore
    {
        public ICollection<IMessageEvent> Events { get; } = new List<IMessageEvent>();

        public void Add(IMessageEvent evt)
        {
            Events.Add(evt);
        }
    }

    

    public interface IMessageEvent
    {
        
    }

    public struct MessageDeleted : IMessageEvent
    {
        public string Id { get; private set; }

        public MessageDeleted(string id)
        {
            Id = id;
        }
    }

    public struct MessageQuacked : IMessageEvent
    {
        public string Id { get; private set; }
        public string Content { get; private set; }

        public MessageQuacked(string id, string content)
        {
            Id = id;
            Content = content;
        }
    }

    public class Message
    {
        private string _id;
        private bool _isDeleted;

        public Message(params IMessageEvent[] events)
        {
            foreach (var @event in events)
            {
                Apply((dynamic)@event);
            }
        }

        private void Apply(MessageQuacked evt)
        {
            _id = evt.Id;
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static Message Quack(IEventsPublisher eventsPublisher, string content)
        {
            var id = Guid.NewGuid().ToString();
            var evt = new MessageQuacked(id, content);
            eventsPublisher.Publish(evt);

            return new Message(evt);
        }

        public string GetId()
        {
            return _id;
        }

        public void Delete(IEventsPublisher eventsPublisher)
        {
            if (_isDeleted) return;

            eventsPublisher.Publish(new MessageDeleted(_id));
        }
    }
}
