using System;
using System.Collections;
using System.Collections.Generic;
using NFluent;
using Xunit;
using Xunit.Sdk;

namespace CqrsLiveCoding
{
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

    public class Timeline
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
        public void GetMessageContentWhenQuackMessage()
        {
            var message = Message.Quack(_eventsStore, Content);

            Check.That(message.GetContent()).IsEqualTo(Content);
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
        private string _content;
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
            _content = evt.Content;
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

        public string GetContent()
        {
            return _content;
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
