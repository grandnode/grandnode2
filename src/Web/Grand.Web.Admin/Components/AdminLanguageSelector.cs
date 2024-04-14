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
    private readonly IWorkContext _workContext;

    public AdminLanguageSelectorViewComponent(
        IWorkContext workContext,
        ILanguageService languageService)
    {
        _workContext = workContext;
        _languageService = languageService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new LanguageSelectorModel {
            CurrentLanguage = _workContext.WorkingLanguage.ToModel(),
            AvailableLanguages = (await _languageService
                    .GetAllLanguages(
                        true,
                        _workContext.CurrentStore.Id))
                .Select(x => x.ToModel())
                .ToList()
        };
        return View(model);
    }
}