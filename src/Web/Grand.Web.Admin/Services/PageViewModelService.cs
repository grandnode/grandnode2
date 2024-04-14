using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Pages;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class PageViewModelService : IPageViewModelService
{
    private readonly IDateTimeService _dateTimeService;
    private readonly ILanguageService _languageService;
    private readonly IPageLayoutService _pageLayoutService;
    private readonly IPageService _pageService;
    private readonly SeoSettings _seoSettings;
    private readonly ISlugService _slugService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;

    public PageViewModelService(
        IPageLayoutService pageLayoutService,
        IPageService pageService,
        ISlugService slugService,
        ITranslationService translationService,
        IStoreService storeService,
        ILanguageService languageService,
        IDateTimeService dateTimeService,
        SeoSettings seoSettings)
    {
        _pageLayoutService = pageLayoutService;
        _pageService = pageService;
        _slugService = slugService;
        _translationService = translationService;
        _storeService = storeService;
        _languageService = languageService;
        _dateTimeService = dateTimeService;
        _seoSettings = seoSettings;
    }

    public virtual async Task<PageListModel> PreparePageListModel()
    {
        var model = new PageListModel();
        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        return model;
    }

    public virtual async Task PrepareLayoutsModel(PageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var layouts = await _pageLayoutService.GetAllPageLayouts();
        foreach (var layout in layouts)
            model.AvailablePageLayouts.Add(new SelectListItem {
                Text = layout.Name,
                Value = layout.Id
            });
    }

    public virtual async Task<Page> InsertPageModel(PageModel model)
    {
        if (!model.IsPasswordProtected) model.Password = null;

        var page = model.ToEntity(_dateTimeService);
        await _pageService.InsertPage(page);
        //search engine name
        model.SeName = await page.ValidateSeName(model.SeName, page.Title ?? page.SystemName, true, _seoSettings,
            _slugService, _languageService);
        page.Locales =
            await model.Locales.ToTranslationProperty(page, x => x.Title, _seoSettings, _slugService, _languageService);
        page.SeName = model.SeName;
        await _pageService.UpdatePage(page);
        await _slugService.SaveSlug(page, model.SeName, "");
        return page;
    }

    public virtual async Task<Page> UpdatePageModel(Page page, PageModel model)
    {
        if (!model.IsPasswordProtected) model.Password = null;
        page = model.ToEntity(page, _dateTimeService);
        page.Locales =
            await model.Locales.ToTranslationProperty(page, x => x.Title, _seoSettings, _slugService, _languageService);
        model.SeName = await page.ValidateSeName(model.SeName, page.Title ?? page.SystemName, true, _seoSettings,
            _slugService, _languageService);
        page.SeName = model.SeName;
        await _pageService.UpdatePage(page);

        //search engine name
        await _slugService.SaveSlug(page, model.SeName, "");
        return page;
    }

    public virtual async Task DeletePage(Page page)
    {
        await _pageService.DeletePage(page);
    }
}