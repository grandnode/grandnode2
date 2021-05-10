using System.Collections.Generic;

namespace Grand.Business.Authentication.Utilities
{
    public partial class TwoFactorCodeSetup
    {
        public TwoFactorCodeSetup()
        {
            CustomValues = new Dictionary<string, string>();
        }
        public IDictionary<string, string> CustomValues { get; set; }
    }
}
