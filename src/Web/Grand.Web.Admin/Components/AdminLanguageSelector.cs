using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Components
{
    public class AdminLanguageSelectorViewComponent : BaseAdminViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;

        public AdminLanguageSelectorViewComponent(
            IWorkContext workContext,
            ILanguageService languageService)
        {
            _workContext = workContext;
            _languageService = languageService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new LanguageSelectorModel();
            model.CurrentLanguage = _workContext.WorkingLanguage.ToModel();
            model.AvailableLanguages = (await _languageService
                .GetAllLanguages(
                   showHidden: true,
                   storeId: _workContext.CurrentStore.Id))
                .Select(x => x.ToModel())
                .ToList();
            return View(model);
        }
    }
}