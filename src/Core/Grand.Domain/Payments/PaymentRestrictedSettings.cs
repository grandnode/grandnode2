using Grand.Domain.Configuration;

namespace Grand.Domain.Payments;

public class PaymentRestrictedSettings : ISettings
{
    public IList<string> Ids { get; set; } = new List<string>();
}