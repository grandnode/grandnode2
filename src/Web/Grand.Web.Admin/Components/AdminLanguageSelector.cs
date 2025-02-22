using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class AdminLanguageSelectorViewComponent : BaseAdminViewComponent
{
    private readonly ILanguageService _languageService;
    private readonly IContextAccessor _contextAccessor;

    public AdminLanguageSelectorViewComponent(
        IContextAccessor contextAccessor,
        ILanguageService languageService)
    {
        _contextAccessor = contextAccessor;
        _languageService = languageService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new LanguageSelectorModel {
            CurrentLanguage = _contextAccessor.WorkContext.WorkingLanguage.ToModel(),
            AvailableLanguages = (await _languageService
                    .GetAllLanguages(
                        true,
                        _contextAccessor.StoreContext.CurrentStore.Id))
                .Select(x => x.ToModel())
                .ToList()
        };
        return View(model);
    }
}