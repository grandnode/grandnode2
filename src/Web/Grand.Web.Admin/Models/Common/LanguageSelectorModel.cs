using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Localization;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Common
{
    public partial class LanguageSelectorModel : BaseModel
    {
        public LanguageSelectorModel()
        {
            AvailableLanguages = new List<LanguageModel>();
        }

        public IList<LanguageModel> AvailableLanguages { get; set; }

        public LanguageModel CurrentLanguage { get; set; }
    }
}