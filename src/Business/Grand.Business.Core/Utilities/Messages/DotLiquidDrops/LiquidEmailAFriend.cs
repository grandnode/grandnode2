using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public partial class LiquidEmailAFriend : Drop
    {
        private readonly string _personalMessage;
        private readonly string _customerEmail;
        private readonly string _friendsEmail;

        public LiquidEmailAFriend(string personalMessage, string customerEmail, string friendsEmail)
        {
            _personalMessage = personalMessage;
            _customerEmail = customerEmail;
            _friendsEmail = friendsEmail;

            AdditionalTokens = new Dictionary<string, string>();
        }

        public string PersonalMessage
        {
            get { return _personalMessage; }
        }

        public string Email
        {
            get { return _customerEmail; }
        }

        public string FriendsEmail {
            get { return _friendsEmail; }
        }

        public IDictionary<string, string> AdditionalTokens { get; set; }
    }
}
