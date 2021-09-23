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
            var eventsStore = new List<object>();
            
            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked("Hello"));
        }
        
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
        public static void Quack(List<object> eventsStore, string message)
        {
            eventsStore.Add(new MessageQuacked(message));
        }
    }
}
