using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.System.Installation;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallLocaleResources()
        {
            //'English' language
            var language = _languageRepository.Table.Single(l => l.Name == "English");

            //save resources
            var filePath = CommonPath.MapPath("App_Data/Resources/DefaultLanguage.xml");
            var localesXml = File.ReadAllText(filePath);
            var translationService = _serviceProvider.GetRequiredService<ITranslationService>();
            await translationService.ImportResourcesFromXmlInstall(language, localesXml);
        }
    }
}
