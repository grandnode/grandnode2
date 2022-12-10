using Grand.Domain.Configuration;

namespace Grand.Domain.Payments
{
    public class PaymentRestrictedSettings : ISettings
    {
        public PaymentRestrictedSettings()
        {
            Ids = new List<string>();
        }
        public IList<string> Ids { get; set; }
    }
}
