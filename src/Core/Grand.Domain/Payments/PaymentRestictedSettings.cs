using Grand.Domain.Configuration;
using System.Collections.Generic;

namespace Grand.Domain.Payments
{
    public class PaymentRestictedSettings : ISettings
    {
        public PaymentRestictedSettings()
        {
            Ids = new List<string>();
        }
        public IList<string> Ids { get; set; }
    }
}
