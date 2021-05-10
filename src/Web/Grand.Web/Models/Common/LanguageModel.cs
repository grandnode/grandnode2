using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Common
{
    public partial class LanguageModel : BaseEntityModel
    {
        public string Name { get; set; }
        public bool Rtl { get; set; }
        public string LanguageCulture { get; set; }
        public string UniqueSeoCode { get; set; }
    }
}