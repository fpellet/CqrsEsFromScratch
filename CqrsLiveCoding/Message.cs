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

            var message = Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore)
                .ContainsExactly(new MessageQuacked(message.GetId(), "Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMesage()
        {
            var eventsStore = new List<object>();
            var message = new Message(new MessageQuacked("A", "Hello"));

            message.Delete(eventsStore);

            Check.That(eventsStore).ContainsExactly(new MessageDeleted("A"));
        }
    }

    public struct MessageDeleted
    {
        public string Id { get; private set; }

        public MessageDeleted(string id)
        {
            Id = id;
        }
    }

    public struct MessageQuacked
    {
        public string Id { get; private set; }
        public string Content { get; private set; }

        public MessageQuacked(string id, string content)
        {
            Id = id;
            Content = content;
        }
    }

    public class Message
    {
        private readonly string _content;
        private string _id;

        public Message(MessageQuacked evt)
        {
            _id = evt.Id;
            _content = evt.Content;
        }

        public static Message Quack(List<object> eventsStore, string content)
        {
            var id = Guid.NewGuid().ToString();
            var evt = new MessageQuacked(id, content);
            eventsStore.Add(evt);

            return new Message(evt);
        }

        public string GetContent()
        {
            return _content;
        }

        public string GetId()
        {
            return _id;
        }

        public void Delete(List<object> eventsStore)
        {
            eventsStore.Add(new MessageDeleted(_id));
        }
    }
}
