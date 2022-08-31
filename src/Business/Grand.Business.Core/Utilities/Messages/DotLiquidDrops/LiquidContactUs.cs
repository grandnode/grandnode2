using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidContactUs : Drop
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

        public string SenderEmail
        {
            get { return senderEmail; }
        }

        public string SenderName
        {
            get { return senderName; }
        }

        public string Body
        {
            get { return body; }
        }

        public string AttributeDescription
        {
            get { return attributeDescription; }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}