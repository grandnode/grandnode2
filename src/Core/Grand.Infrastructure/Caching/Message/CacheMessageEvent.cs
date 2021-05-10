
namespace Grand.Infrastructure.Caching.Message
{
    public class CacheMessageEvent 
    {
        public string ClientId { get; set; }
        public string Key { get; set; }
        public int MessageType { get; set; }
    }
}
