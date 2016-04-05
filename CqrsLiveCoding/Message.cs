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
        public static void Quack(List<object> history, string content)
        {
            history.Add(new MessageQuacked(content));
        }
    }
}
