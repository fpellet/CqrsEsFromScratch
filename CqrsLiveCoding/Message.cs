using System.Collections;
using System.Collections.Generic;
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
        public Message(IEnumerable<object> history)
        {
        }

        public static void Quack(List<object> history, string content)
        {
            history.Add(new MessageQuacked(content));
        }

        public void Delete(List<object> history)
        {
            history.Add(new MessageDeleted());
        }
    }
}
