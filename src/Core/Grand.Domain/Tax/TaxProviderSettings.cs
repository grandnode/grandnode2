using Grand.Domain.Configuration;

namespace Grand.Domain.Tax
{
    public class TaxProviderSettings : ISettings
    {
        public string ActiveTaxProviderSystemName { get; set; }
    }
}