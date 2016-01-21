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

            timeline.Handle(new MessageQuacked("Hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("Hello"));
        }
    }

    public class Timeline
    {
        private IList<TimelineMessage> _messages = new List<TimelineMessage>();

        public IEnumerable<TimelineMessage> Messages => _messages;

        public void Handle(MessageQuacked evt)
        {
            _messages.Add(new TimelineMessage(evt.Content));
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
        [Fact]
        public void GetMessageContentWhenQuackMessage()
        {
            var message = Message.Quack(new EventsStoreFake(), "Hello");

            Check.That(message.GetContent()).IsEqualTo("Hello");
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var eventsStore = new EventsStoreFake();

            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore.Events).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new EventsStoreFake();
            eventsStore.Add(new MessageQuacked("Hello"));

            var message = new Message(eventsStore.Events);

            message.Delete(eventsStore);

            Check.That(eventsStore.Events).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new EventsStoreFake();
            eventsStore.Add(new MessageQuacked("Hello"));
            eventsStore.Add(new MessageDeleted());

            var message = new Message(eventsStore.Events);

            message.Delete(eventsStore);

            Check.That(eventsStore.Events).HasSize(2);
        }
    }

    public interface IEventsStore
    {
        void Add(IMessageEvent evt);
    }

    public class EventsStoreFake : IEventsStore
    {
        private IList<IMessageEvent> _events = new List<IMessageEvent>();

        public IEnumerable<IMessageEvent> Events => _events;

        public void Add(IMessageEvent evt)
        {
            _events.Add(evt);
        }
    }

    public interface IMessageEvent
    {
        
    }

    public struct MessageDeleted : IMessageEvent
    {
    }

    public struct MessageQuacked : IMessageEvent
    {
        public string Content { get; private set; }

        public MessageQuacked(string content)
        {
            Content = content;
        }
    }

    public class Message
    {
        private string _content;
        private bool _isDeleted = false;

        public Message(IEnumerable<IMessageEvent> events)
        {
            foreach (var @event in events)
            {
                Apply((dynamic) @event);
            }
        }

        private void Apply(MessageQuacked evt)
        {
            _content = evt.Content;
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static Message Quack(IEventsStore eventsStore, string content)
        {
            var evt = new MessageQuacked(content);
            eventsStore.Add(evt);
            return new Message(new IMessageEvent[]{ evt });
        }

        public string GetContent()
        {
            return _content;
        }

        public void Delete(IEventsStore eventsStore)
        {
            if (_isDeleted) return;

            eventsStore.Add(new MessageDeleted());
        }
    }
}
