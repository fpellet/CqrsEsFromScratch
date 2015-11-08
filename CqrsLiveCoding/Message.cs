using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;
using Xunit.Sdk;

namespace CqrsLiveCoding
{
    public class TimelineShould
    {
        private const string Id = "A";
        private const string Content = "Hello";

        [Fact]
        public void AddMessageinTimelineWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.Handle(new MessageQuacked(Id, Content));

            Check.That(timeline.Messages).Contains(new TimelineMessage(Content));
        }

        [Fact]
        public void AddMessageInTimelineWhenQuackMessage()
        {
            var timeline = new Timeline();
            var eventsStore = new EventsStoreFake();
            eventsStore.Subscribe(timeline);

            Message.Quack(eventsStore, Content);

            Check.That(timeline.Messages).Contains(new TimelineMessage(Content));
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

    public class Timeline : IEventHandler<MessageQuacked>
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

        public MessageShould()
        {
            _eventsStore = new EventsStoreFake();
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var message = Message.Quack(_eventsStore, Content);

            Check.That(_eventsStore.Events)
                .ContainsExactly(new MessageQuacked(message.GetId(), Content));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMesage()
        {
            var message = new Message(new MessageQuacked(Id, Content));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events).ContainsExactly(new MessageDeleted(Id));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var message = new Message(new MessageQuacked(Id, Content), new MessageDeleted(Id));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events).IsEmpty();
        }
    }

    public interface IEventsStore
    {
        void Add<TEvent>(TEvent evt) where TEvent : IMessageEvent;
    }

    public interface IEventHandler
    {
        
    }

    public interface IEventHandler<TEvent> : IEventHandler
        where TEvent: IMessageEvent
    {
        void Handle(TEvent evt);
    }

    public class EventsStoreFake : IEventsStore
    {
        private readonly ICollection<IEventHandler> _handlers = new List<IEventHandler>();
        public ICollection<IMessageEvent> Events { get; } = new List<IMessageEvent>();

        public void Add<TEvent>(TEvent evt) where TEvent: IMessageEvent
        {
            Events.Add(evt);

            foreach (var handler in _handlers.OfType<IEventHandler<TEvent>>())
            {
                handler.Handle(evt);
            }
        }

        public void Subscribe(IEventHandler handler)
        {
            _handlers.Add(handler);
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

        public static Message Quack(IEventsStore eventsStore, string content)
        {
            var id = Guid.NewGuid().ToString();
            var evt = new MessageQuacked(id, content);
            eventsStore.Add(evt);

            return new Message(evt);
        }

        public string GetId()
        {
            return _id;
        }

        public void Delete(IEventsStore eventsStore)
        {
            if (_isDeleted) return;

            eventsStore.Add(new MessageDeleted(_id));
        }
    }
}
