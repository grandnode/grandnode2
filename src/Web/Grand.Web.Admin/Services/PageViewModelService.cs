using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Pages;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Pages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class PageViewModelService : IPageViewModelService
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IPageLayoutService _pageLayoutService;
    private readonly IPageService _pageService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly ISeNameService _seNameService;
    
    public PageViewModelService(
        IPageLayoutService pageLayoutService,
        IPageService pageService,
        ITranslationService translationService,
        IStoreService storeService,
        IDateTimeService dateTimeService,
        ISeNameService seNameService)
    {
        _pageLayoutService = pageLayoutService;
        _pageService = pageService;
        _translationService = translationService;
        _storeService = storeService;
        _dateTimeService = dateTimeService;
        _seNameService = seNameService;
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
        //search engine name
        page.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, page, x => x.Title);
        page.SeName = await _seNameService.ValidateSeName(page, model.SeName, page.Title ?? page.SystemName, true);

        await _pageService.InsertPage(page);
        await _seNameService.SaveSeName(page);
        
        return page;
    }

    public virtual async Task<Page> UpdatePageModel(Page page, PageModel model)
    {
        if (!model.IsPasswordProtected) model.Password = null;
        page = model.ToEntity(page, _dateTimeService);
        page.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, page, x => x.Title);
        page.SeName = await _seNameService.ValidateSeName(page, model.SeName, page.Title ?? page.SystemName, true);
        
        await _pageService.UpdatePage(page);
        //search engine name
        await _seNameService.SaveSeName(page);
        
        return page;
    }

    public virtual async Task DeletePage(Page page)
    {
        await _pageService.DeletePage(page);
    }
}