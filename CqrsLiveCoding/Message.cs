using System;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void GetMessageContentWhenQuackMessage()
        {
            var message = Message.Quack("Hello");

            Check.That(message.GetContent()).IsEqualTo("Hello");
        }
    }

    public class Message
    {
        private readonly string _content;

        private Message(string content)
        {
            _content = content;
        }

        public static Message Quack(string content)
        {
            return new Message(content);
        }

        public string GetContent()
        {
            return _content;
        }
    }
}
