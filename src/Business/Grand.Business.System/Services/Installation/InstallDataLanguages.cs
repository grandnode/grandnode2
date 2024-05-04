using Grand.Domain.Localization;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual async Task InstallLanguages()
    {
        var language = new Language {
            Name = "English",
            LanguageCulture = "en-US",
            UniqueSeoCode = "en",
            FlagImageFileName = "us.png",
            Published = true,
            DisplayOrder = 1
        };
        await _languageRepository.InsertAsync(language);
    }
}