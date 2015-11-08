using System;
using System.Collections;
using System.Collections.Generic;
using NFluent;
using Xunit;

namespace CqrsLiveCoding
{
    public class MessageShould
    {
        private const string Id = "A";
        private const string Content = "Hello";

        private readonly List<object> _eventsStore;

        public MessageShould()
        {
            _eventsStore = new List<object>();
        }

        [Fact]
        public void GetMessageContentWhenQuackMessage()
        {
            var message = Message.Quack(_eventsStore, Content);

            Check.That(message.GetContent()).IsEqualTo(Content);
        }

        [Fact]
        public void RaiseMessageQuackedWhenQuackMessage()
        {
            var message = Message.Quack(_eventsStore, Content);

            Check.That(_eventsStore)
                .ContainsExactly(new MessageQuacked(message.GetId(), Content));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMesage()
        {
            var message = new Message(new MessageQuacked(Id, Content));

            message.Delete(_eventsStore);

            Check.That(_eventsStore).ContainsExactly(new MessageDeleted(Id));
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
