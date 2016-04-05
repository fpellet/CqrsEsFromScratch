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
}
