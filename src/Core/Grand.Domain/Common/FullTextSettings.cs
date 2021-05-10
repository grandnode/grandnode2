using Grand.Domain.Configuration;

namespace Grand.Domain.Common
{
    public class FullTextSettings : ISettings
    {
        public bool UseFullTextSearch { get; set; }
    }
}