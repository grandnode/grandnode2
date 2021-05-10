
namespace Grand.Infrastructure.Caching.Message
{
    public class MessageEvent : IMessageEvent
    {
        public string Key { get; set; }
        public int MessageType { get; set; }
    }
}
