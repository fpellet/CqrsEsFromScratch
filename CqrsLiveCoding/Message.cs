using System.Collections.Generic;
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
        public Message(List<object> history)
        {
            
        }

        public static void Quack(List<object> eventsStore, string message)
        {
            eventsStore.Add(new MessageQuacked(message));
        }

        public void Delete(List<object> eventsStore)
        {
            eventsStore.Add(new MessageDeleted());
        }
    }
}
