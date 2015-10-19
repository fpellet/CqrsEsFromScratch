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
            var eventsStore = new List<object>();

            var id = Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked(id, "Hello"));
        }

        [Fact]
        public void RaiseMessageDeletedWhenDeleteMessage()
        {
            var eventsStore = new List<object>();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"));

            message.Delete(eventsStore);

            Check.That(eventsStore).ContainsExactly(new MessageDeleted(messageId));
        }

        [Fact]
        public void NotRaiseMessageDeletedWhenDeleteDeletedMessage()
        {
            var eventsStore = new List<object>();
            var messageId = "MessageA";
            var message = new Message(new MessageQuacked(messageId, "Hello"), new MessageDeleted(messageId));

            message.Delete(eventsStore);

            Check.That(eventsStore).IsEmpty();
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

    public class Message
    {
        private string _id;

        public Message(MessageQuacked messageQuacked)
        {
            _id = messageQuacked.Id;
        }

        public static string Quack(List<object> eventsStore, string message)
        {
            var id = Guid.NewGuid().ToString();
            eventsStore.Add(new MessageQuacked(id, message));

            return id;
        }

        public void Delete(List<object> eventsStore)
        {
            eventsStore.Add(new MessageDeleted(_id));
        }
    }

    public struct MessageQuacked
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
