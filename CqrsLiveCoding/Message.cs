using System;
using System.Collections;
using System.Collections.Generic;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        [Fact]
        public void GetMessageContentWhenQuackMessage()
        {
            var message = Message.Quack(new List<object>(), "Hello");

            Check.That(message.GetContent()).IsEqualTo("Hello");
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var eventsStore = new List<object>();

            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked("Hello"));
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
        private readonly string _content;

        private Message(string content)
        {
            _content = content;
        }

        public static Message Quack(List<object> eventsStore, string content)
        {
            eventsStore.Add(new MessageQuacked(content));

            return new Message(content);
        }

        public string GetContent()
        {
            return _content;
        }
    }
}
