
namespace Grand.Infrastructure.Caching.Message
{
    public interface IMessageEvent
    {
        string Key { get; set; }
        int MessageType { get; set; }
    }
}
