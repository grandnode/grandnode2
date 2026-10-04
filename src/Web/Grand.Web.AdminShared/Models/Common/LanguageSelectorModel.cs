using Grand.Infrastructure.Models;
using Grand.Web.AdminShared.Models.Localization;

namespace Grand.Web.AdminShared.Models.Common;

public class LanguageSelectorModel : BaseModel
{
    public IList<LanguageModel> AvailableLanguages { get; set; } = new List<LanguageModel>();

    public LanguageModel CurrentLanguage { get; set; }
}