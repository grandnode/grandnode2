using DotLiquid;

namespace Grand.Business.Messages.DotLiquidDrops
{
    public partial class LiquidEmail : Drop
    {
        private string _emailId { get; set; }

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
