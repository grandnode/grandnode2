using DotLiquid;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops
{
    public class LiquidEmail : Drop
    {
        private readonly string _emailId;

        public LiquidEmail(string emailId)
        {
            _emailId = emailId;
        }

        public string Id
        {
            get { return _emailId; }
        }
    }
}
