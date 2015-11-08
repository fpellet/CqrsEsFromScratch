using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class QuackCounterShould
    {
        [Fact]
        public void IncrementCounterWhenMessageQuacked()
        {
            var counter = new QuackCounter();

            counter.Handle(new MessageQuacked("A", "Hello"));

            Check.That(counter.Nb).IsEqualTo(1);
        }

        [Fact]
        public void DecrementCounterWhenMessageDeleted()
        {
            var counter = new QuackCounter();

            counter.Handle(new MessageDeleted("A"));

            Check.That(counter.Nb).IsEqualTo(-1);
        }
    }

    public class QuackCounter
    {
        public int Nb { get; private set; }
        public void Handle(MessageQuacked evt)
        {
            Nb++;
        }

        public void Handle(MessageDeleted evt)
        {
            Nb--;
        }
    }

    public class TimelineShould
    {
        [Fact]
        public void AddMessageInTimelineWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.Handle(new MessageQuacked("1", "Hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
        }

        [Fact]
        public void AddMessageInTimelineWhenQuackMessage()
        {
            var timeline = new Timeline();
            var eventsStore = new MemoryEventsStore();
            eventsStore.Subscribe(timeline);

            Message.Quack(eventsStore, "Hello");

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
        }
    }

    public interface IEventHandler
    {
        
    }

    public interface IEventHandler<TEvent> : IEventHandler where TEvent : IDomainEvent
    {
        void Handle(TEvent evt);
    }

    public class Timeline : IEventHandler<MessageQuacked>
    {
        public IList<TimelineMessage> Messages { get; } = new List<TimelineMessage>();
        public void Handle(MessageQuacked evt)
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
        private const string MessageId = "M1";
        private readonly MemoryEventsStore _eventsStore;

        public MessageShould()
        {
            _eventsStore = new MemoryEventsStore();
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var message = Message.Quack(_eventsStore, "Hello");

            Check.That(_eventsStore.Events).ContainsExactly(new MessageQuacked(message.GetId(), "Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var message = new Message(new MessageQuacked(MessageId, "Hello"));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events).ContainsExactly(new MessageDeleted(MessageId));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var message = new Message(new MessageQuacked(MessageId, "Hello"), new MessageDeleted(MessageId));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events).IsEmpty();
        }
    }

    public interface IEventsStore
    {
        void Add<T>(T evt) where T : IDomainEvent;
    }

    public class MemoryEventsStore : IEventsStore
    {
        private IList<IEventHandler> _handlers = new List<IEventHandler>();
        public IList<IDomainEvent> Events { get; } = new List<IDomainEvent>();

        public void Add<T>(T evt) where T: IDomainEvent
        {
            Events.Add(evt);

            foreach (var handler in _handlers.OfType<IEventHandler<T>>())
            {
                handler.Handle(evt);
            }
        }

        public void Subscribe(IEventHandler handler)
        {
            _handlers.Add(handler);
        }
    }

    public interface IDomainEvent
    {
        
    }

    public struct MessageDeleted : IDomainEvent
    {
        public string MessageId { get; private set; }

        public MessageDeleted(string messageId)
        {
            MessageId = messageId;
        }
    }

    public struct MessageQuacked : IDomainEvent
    {
        public string MessageId { get; private set; }

        public string Content { get; private set; }

        public MessageQuacked(string messageId, string content)
        {
            MessageId = messageId;
            Content = content;
        }
    }

    public class Message
    {
        private string _id;
        private bool _isDeleted;

        public Message(params IDomainEvent[] events)
        {
            foreach (var @event in events)
            {
                Apply((dynamic)@event);
            }
        }

        private void Apply(MessageQuacked evt)
        {
            _id = evt.MessageId;
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static Message Quack(IEventsStore eventsStore, string content)
        {
            var evt = new MessageQuacked(Guid.NewGuid().ToString(), content);
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
