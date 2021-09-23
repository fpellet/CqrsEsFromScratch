using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        private readonly List<IDomainEvent> _history = new List<IDomainEvent>();

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var history = _history;
            
            Message.Quack(history, "Hello");

            Check.That(history).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var history = _history;
            history.Add(new MessageQuacked("Hello"));
            var message = new Message(history);

            message.Delete(history);

            Check.That(history).Contains(new MessageDeleted());
        }

        [Fact]
        public void NotRaiseDeletedWhenDeleteDeletedMessage()
        {
            var history = _history;
            history.Add(new MessageQuacked("Hello"));
            history.Add(new MessageDeleted());
            var message = new Message(history);
            
            message.Delete(history);

            Check.That(history.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            var history = _history;
            history.Add(new MessageQuacked("Hello"));
            var message = new Message(history);
            
            message.Delete(history);
            message.Delete(history);

            Check.That(history.OfType<MessageDeleted>()).HasSize(1);
        }
    }

    public struct MessageDeleted : IDomainEvent
    {
    }

    public interface IDomainEvent
    {
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

        public static void Quack(List<IDomainEvent> eventsStore, string message)
        {
            eventsStore.Add(new MessageQuacked(message));
        }

        public void Delete(List<IDomainEvent> eventsStore)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            eventsStore.Add(evt);
            Apply(evt);
        }
    }
}
