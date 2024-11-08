using Grand.Business.Core.Interfaces.Common.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual async Task InstallLocaleResources()
    {
        //'English' language
        var language = _languageRepository.Table.Single(l => l.Name == "English");

        //save resources
        var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "App_Data/Resources/DefaultLanguage.xml");
        var localesXml = File.ReadAllText(filePath);
        var translationService = _serviceProvider.GetRequiredService<ITranslationService>();
        await translationService.ImportResourcesFromXmlInstall(language, localesXml);
    }
}