using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Localization;

namespace Grand.Web.Admin.Models.Common;

public class LanguageSelectorModel : BaseModel
{
    public IList<LanguageModel> AvailableLanguages { get; set; } = new List<LanguageModel>();

    public LanguageModel CurrentLanguage { get; set; }
}