using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public class LiquidContactUs : Drop
    {
        private readonly string senderEmail;
        private readonly string senderName;
        private readonly string body;
        private readonly string attributeDescription;

        public LiquidContactUs(string senderEmail, string senderName, string body, string attributeDescription)
        {
            this.senderEmail = senderEmail;
            this.senderName = senderName;
            this.body = body;
            this.attributeDescription = attributeDescription;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string SenderEmail => senderEmail;

        public string SenderName => senderName;

        public string Body => body;

        public string AttributeDescription => attributeDescription;

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}