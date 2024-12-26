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
    private readonly IWorkContextAccessor _workContextAccessor;

    public AdminLanguageSelectorViewComponent(
        IWorkContextAccessor workContextAccessor,
        ILanguageService languageService)
    {
        _workContextAccessor = workContextAccessor;
        _languageService = languageService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new LanguageSelectorModel {
            CurrentLanguage = _workContextAccessor.WorkContext.WorkingLanguage.ToModel(),
            AvailableLanguages = (await _languageService
                    .GetAllLanguages(
                        true,
                        _workContextAccessor.WorkContext.CurrentStore.Id))
                .Select(x => x.ToModel())
                .ToList()
        };
        return View(model);
    }
}