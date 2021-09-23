using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var history = new List<object>();
            
            Message.Quack(history, "Hello");

            Check.That(history).ContainsExactly(new MessageQuacked("Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var history = new List<object>();
            history.Add(new MessageQuacked("Hello"));
            var message = new Message(history);

            message.Delete(history);

            Check.That(history).Contains(new MessageDeleted());
        }

        [Fact]
        public void RaiseNothingWhenDeleteDeletedMessage()
        {
            var history = new List<object>();
            history.Add(new MessageQuacked("Hello"));
            history.Add(new MessageDeleted());
            var message = new Message(history);
            
            message.Delete(history);

            Check.That(history.OfType<MessageDeleted>()).HasSize(1);
        }
    }

    public struct MessageDeleted
    {
    }

    public struct MessageQuacked
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

        public Message(List<object> history)
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

        public static void Quack(List<object> eventsStore, string message)
        {
            eventsStore.Add(new MessageQuacked(message));
        }

        public void Delete(List<object> eventsStore)
        {
            if (_isDeleted) return;
            
            eventsStore.Add(new MessageDeleted());
        }
    }
}
