using DotLiquid;
using System.Collections.Generic;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidEmailAFriend : Drop
    {
        private string _personalMessage;
        private string _customerEmail;
        private string _friendsEmail;

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
