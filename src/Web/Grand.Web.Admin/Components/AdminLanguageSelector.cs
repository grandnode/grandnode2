using AutoMapper;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Localization;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class AdminLanguageSelectorViewComponent : BaseAdminViewComponent
{
    private readonly ILanguageService _languageService;
    private readonly IWorkContext _workContext;
    private readonly IMapper _mapper;

    public AdminLanguageSelectorViewComponent(
        IWorkContext workContext,
        ILanguageService languageService,
        IMapper mapper)
    {
        _workContext = workContext;
        _languageService = languageService;
        _mapper = mapper;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new LanguageSelectorModel {
            CurrentLanguage = _mapper.Map<LanguageModel>(_workContext.WorkingLanguage),
            AvailableLanguages = (await _languageService
                    .GetAllLanguages(
                        true,
                        _workContext.CurrentStore.Id))
                .Select(_mapper.Map<LanguageModel>)
                .ToList()
        };
        return View(model);
    }
}