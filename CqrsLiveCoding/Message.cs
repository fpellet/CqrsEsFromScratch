using System;
using System.Collections;
using System.Collections.Generic;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class TimelineShould
    {
        [Fact]
        public void AddMessageInTimelineWhenMessageQuacked()
        {
            var timeline = new Timeline();

            timeline.Handle(new MessageQuacked("A", "Hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
        }
    }

    public class Timeline
    {
        public IList<TimelineMessage> Messages { get; } = new List<TimelineMessage>();

        public void Handle(MessageQuacked evt)
        {
            Messages.Add(new TimelineMessage(evt.Content));
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
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var message = new Message(new MessageQuacked(Id, Content));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events)
                .ContainsExactly(new MessageDeleted(Id));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var message = new Message(
                new MessageQuacked(Id, Content), 
                new MessageDeleted(Id));

            message.Delete(_eventsStore);

            Check.That(_eventsStore.Events).IsEmpty();
        }
    }

    public interface IEventsStore
    {
        void Add(IMessageEvent evt);
    }

    public class EventsStoreFake : IEventsStore
    {
        public IList<IMessageEvent> Events { get; } = new List<IMessageEvent>();


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
        public string Id { get; }

        public MessageDeleted(string id)
        {
            Id = id;
        }
    }

    public struct MessageQuacked : IMessageEvent
    {
        public string Id { get;  }
        public string Content { get;  }

        public MessageQuacked(string id, string content)
        {
            Id = id;
            Content = content;
        }
    }

    public class Message
    {
        private string _id;
        private bool _isDeleted = false;

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
