using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            timeline.Handle(new MessageQuacked("MessageA", "hello"));

            Check.That(timeline.Messages).Contains(new TimelineMessage("hello"));
        }
    }

    public struct TimelineMessage
    {
        public string Message { get; private set; }

        public TimelineMessage(string message)
        {
            Message = message;
        }
    }

    public class Timeline
    {
        public IList<TimelineMessage> Messages { get; } = new List<TimelineMessage>();

        public void Handle(MessageQuacked evt)
        {
            Messages.Add(new TimelineMessage(evt.Message));
        }
    }

    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var eventsStore = new EventsStore();

            var id = Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore.Events).ContainsExactly(new MessageQuacked(id, "Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new EventsStore();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"));

            message.Delete(eventsStore);

            Check.That(eventsStore.Events).ContainsExactly(new MessageDeleted(messageId));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new EventsStore();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"), new MessageDeleted(messageId));

            message.Delete(eventsStore);

            Check.That(eventsStore.Events).IsEmpty();
        }
    }

    public class EventsStore : IEventsStore
    {
        public IList<IDomainEvent> Events { get; } = new List<IDomainEvent>();


        public void Push(IDomainEvent evt)
        {
            Events.Add(evt);
        }
    }

    public interface IEventsStore
    {
        void Push(IDomainEvent evt);
    }

    public interface IDomainEvent
    {
    }

    public struct MessageDeleted : IDomainEvent
    {
        public string Id { get; private set; }

        public MessageDeleted(string id)
        {
            Id = id;
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
                Apply((dynamic) @event);
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

        public static string Quack(IEventsStore eventsStore, string message)
        {
            var id = Guid.NewGuid().ToString();
            eventsStore.Push(new MessageQuacked(id, message));

            return id;
        }

        public void Delete(IEventsStore eventsStore)
        {
            if (_isDeleted) return;

            eventsStore.Push(new MessageDeleted(_id));
        }
    }

    public struct MessageQuacked : IDomainEvent
    {
        public string Id { get; private set; }
        public string Message { get; private set; }

        public MessageQuacked(string id, string message)
        {
            Id = id;
            Message = message;
        }
    }
}
