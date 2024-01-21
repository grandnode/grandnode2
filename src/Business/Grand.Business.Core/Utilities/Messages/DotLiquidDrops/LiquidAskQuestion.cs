using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public class LiquidAskQuestion : Drop
    {
        private readonly string message;
        private readonly string email;
        private readonly string fullName;
        private readonly string phone;

        public LiquidAskQuestion(string message, string email, string fullName, string phone)
        {
            this.message = message;
            this.email = email;
            this.fullName = fullName;
            this.phone = phone;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string Email => email;

        public string Message => message;

        public string FullName => fullName;

        public string Phone => phone;

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}