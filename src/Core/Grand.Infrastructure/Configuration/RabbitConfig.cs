namespace Grand.Infrastructure.Configuration
{
    public partial class RabbitConfig
    {
        public bool RabbitEnabled { get; set; }
        public bool RabbitCachePubSubEnabled { get; set; }
        public string RabbitHostName { get; set; }
        public string RabbitVirtualHost { get; set; }
        public string RabbitUsername { get; set; }
        public string RabbitPassword { get; set; }
        public string RabbitCacheReceiveEndpoint { get; set; }
    }
}
