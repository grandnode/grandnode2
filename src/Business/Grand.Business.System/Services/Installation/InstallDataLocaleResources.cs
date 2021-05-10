using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.System.Interfaces.Installation;
using Grand.SharedKernel.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
