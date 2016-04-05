using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void RaiseMessageQuackWhenQuackMessage()
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
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var history = new List<object>();
            history.Add(new MessageQuacked("Hello"));
            history.Add(new MessageDeleted());
            var message = new Message(history);

            message.Delete(history);

            Check.That(history.OfType<MessageDeleted>()).HasSize(1);
        }

        [Fact]
        public void NotRaiseDeletedWhenTwiceDeleteMessage()
        {
            var history = new List<object>();
            history.Add(new MessageQuacked("Hello"));
            var message = new Message(history);

            message.Delete(history);
            message.Delete(history);

            Check.That(history.OfType<MessageDeleted>()).HasSize(1);
        }
    }

    public struct MessageDeleted
    {
    }

    public struct MessageQuacked
    {
        public string Content { get; private set; }

        public MessageQuacked(string content)
        {
            Content = content;
        }
    }

    public class Message
    {
        private bool _isDeleted = false;

        public Message(IEnumerable<object> history)
        {
            foreach (var evt in history)
            {
                if (evt is MessageDeleted)
                {
                    Apply((MessageDeleted)evt);
                }
            }
        }

        private void Apply(MessageDeleted evt)
        {
            _isDeleted = true;
        }

        public static void Quack(List<object> history, string content)
        {
            history.Add(new MessageQuacked(content));
        }

        public void Delete(List<object> history)
        {
            if (_isDeleted) return;

            var evt = new MessageDeleted();
            history.Add(evt);
            Apply(evt);
        }
    }
}
