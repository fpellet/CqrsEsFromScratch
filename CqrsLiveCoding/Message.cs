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
}
