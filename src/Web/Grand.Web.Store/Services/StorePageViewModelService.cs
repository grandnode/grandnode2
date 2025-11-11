using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Extensions.Mapping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Pages;

namespace Grand.Web.Store.Services;

/// <summary>
/// Store-specific implementation of IPageViewModelService
/// Provides Store-specific restrictions and functionality
/// </summary>
public class StorePageViewModelService : IPageViewModelService
{
    private readonly IPageService _pageService;
    private readonly IPageLayoutService _pageLayoutService;
    private readonly ITranslationService _translationService;
    private readonly ISeNameService _seNameService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IContextAccessor _contextAccessor;

    public StorePageViewModelService(
        IPageService pageService,
        IPageLayoutService pageLayoutService,
        ITranslationService translationService,
        ISeNameService seNameService,
        IDateTimeService dateTimeService,
        IContextAccessor contextAccessor)
    {
        _pageService = pageService;
        _pageLayoutService = pageLayoutService;
        _translationService = translationService;
        _seNameService = seNameService;
        _dateTimeService = dateTimeService;
        _contextAccessor = contextAccessor;
    }

    public virtual Task<PageListModel> PreparePageListModel()
    {
        var model = new PageListModel();
        return Task.FromResult(model);
    }

    public virtual async Task PrepareLayoutsModel(PageModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var layouts = await _pageLayoutService.GetAllPageLayouts();
        foreach (var layout in layouts)
            model.AvailablePageLayouts.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem {
                Text = layout.Name,
                Value = layout.Id
            });
    }

    public virtual async Task<Page> InsertPageModel(PageModel model)
    {
        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Convert model to entity
        var page = model.ToEntity(_dateTimeService);
        
        // Store-specific: Always limit to current store and don't allow SystemName
        page.SystemName = null; // Never allow setting SystemName from Store UI
        page.LimitedToStores = true;
        page.Stores = new List<string> { storeId };
        
        // Handle locales and SeName
        page.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, page, x => x.Title);
        page.SeName = await _seNameService.ValidateSeName(page, model.SeName, page.Title, true);

        await _pageService.InsertPage(page);
        await _seNameService.SaveSeName(page);
        
        return page;
    }

    public virtual async Task<Page> UpdatePageModel(Page page, PageModel model)
    {
        // Prevent editing system pages (pages with SystemName and no store restrictions)
        if (!string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any())
        {
            throw new InvalidOperationException("Cannot edit system pages. Please copy the page first.");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Ensure the page belongs to the current store
        if (!page.Stores.Contains(storeId))
        {
            throw new InvalidOperationException("Cannot edit a page that doesn't belong to this store.");
        }

        // Update page properties
        page = model.ToEntity(page, _dateTimeService);
        
        // Store-specific: Maintain store restrictions and don't allow changing SystemName
        page.SystemName = null; // Never allow setting SystemName from Store UI
        page.LimitedToStores = true;
        page.Stores = new List<string> { storeId };
        
        // Handle locales and SeName
        page.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, page, x => x.Title);
        page.SeName = await _seNameService.ValidateSeName(page, model.SeName, page.Title, true);
        
        await _pageService.UpdatePage(page);
        await _seNameService.SaveSeName(page);
        
        return page;
    }

    public virtual async Task DeletePage(Page page)
    {
        // Prevent deletion of system pages
        if (!string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any())
        {
            throw new InvalidOperationException("Cannot delete system pages.");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;
        
        // Ensure the page belongs to the current store
        if (!page.Stores.Contains(storeId))
        {
            throw new InvalidOperationException("Cannot delete a page that doesn't belong to this store.");
        }

        await _pageService.DeletePage(page);
    }

    /// <summary>
    /// Store-specific method: Copy a page (typically a system page) for the current store
    /// </summary>
    public virtual async Task<Page> CopyPageModel(string sourcePageId)
    {
        var sourcePage = await _pageService.GetPageById(sourcePageId);
        if (sourcePage == null)
        {
            throw new ArgumentException("Source page not found");
        }

        var storeId = _contextAccessor.StoreContext.CurrentStore.Id;

        var newPage = new Page
        {
            Title = sourcePage.Title + " (Copy)",
            Body = sourcePage.Body,
            PageLayoutId = sourcePage.PageLayoutId,
            Published = false, // Start unpublished
            DisplayOrder = sourcePage.DisplayOrder,
            IncludeInSitemap = sourcePage.IncludeInSitemap,
            IncludeInMenu = sourcePage.IncludeInMenu,
            IncludeInFooterRow1 = sourcePage.IncludeInFooterRow1,
            IncludeInFooterRow2 = sourcePage.IncludeInFooterRow2,
            IncludeInFooterRow3 = sourcePage.IncludeInFooterRow3,
            IsPasswordProtected = false, // Don't copy password
            Password = null,
            AccessibleWhenStoreClosed = sourcePage.AccessibleWhenStoreClosed,
            MetaKeywords = sourcePage.MetaKeywords,
            MetaDescription = sourcePage.MetaDescription,
            MetaTitle = sourcePage.MetaTitle,
            LimitedToStores = true,
            Stores = new List<string> { storeId },
            SystemName = null // Don't copy system name
        };

        // Copy locales
        foreach (var locale in sourcePage.Locales)
        {
            newPage.Locales.Add(new Domain.Localization.TranslationEntity
            {
                LanguageId = locale.LanguageId,
                LocaleKey = locale.LocaleKey,
                LocaleValue = locale.LocaleValue
            });
        }

        newPage.SeName = await _seNameService.ValidateSeName(newPage, "", newPage.Title, true);

        await _pageService.InsertPage(newPage);
        await _seNameService.SaveSeName(newPage);

        return newPage;
    }
}
