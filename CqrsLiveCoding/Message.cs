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
            var eventsStore = new List<object>();

            Message.Quack(eventsStore, "Hello");

            Check.That(eventsStore).ContainsExactly(new MessageQuacked("Hello"));
        }
    }
}
