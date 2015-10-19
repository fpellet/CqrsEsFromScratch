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
    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var eventsStore = new List<IDomainEvent>();

            var id = Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked(id, "Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new List<IDomainEvent>();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"));

            message.Delete(eventsStore);

            Check.That(eventsStore).ContainsExactly(new MessageDeleted(messageId));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new List<IDomainEvent>();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"), new MessageDeleted(messageId));

            message.Delete(eventsStore);

            Check.That(eventsStore).IsEmpty();
        }
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

        public static string Quack(List<IDomainEvent> eventsStore, string message)
        {
            var id = Guid.NewGuid().ToString();
            eventsStore.Add(new MessageQuacked(id, message));

            return id;
        }

        public void Delete(List<IDomainEvent> eventsStore)
        {
            if (_isDeleted) return;

            eventsStore.Add(new MessageDeleted(_id));
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
