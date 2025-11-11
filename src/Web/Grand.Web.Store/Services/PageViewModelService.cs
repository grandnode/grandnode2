using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Web.Store.Interfaces;
using Grand.Web.Store.Models.Pages;

namespace Grand.Web.Store.Services;

public class PageViewModelService : IPageViewModelService
{
    private readonly IPageService _pageService;
    private readonly IPageLayoutService _pageLayoutService;
    private readonly ITranslationService _translationService;
    private readonly ISeNameService _seNameService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IContextAccessor _contextAccessor;

    public PageViewModelService(
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

    public virtual async Task<PageModel> PreparePageModel(string storeId)
    {
        var model = new PageModel
        {
            DisplayOrder = 1,
            Published = true
        };
        return await Task.FromResult(model);
    }

    public virtual async Task<PageModel> PreparePageModel(Page page, string storeId)
    {
        var model = new PageModel
        {
            Id = page.Id,
            SystemName = page.SystemName,
            Title = page.Title,
            Body = page.Body,
            PageLayoutId = page.PageLayoutId,
            Published = page.Published,
            DisplayOrder = page.DisplayOrder,
            IsSystemPage = !string.IsNullOrEmpty(page.SystemName) && 
                          !page.Stores.Any() // System pages have no store restrictions
        };

        // Add locales
        foreach (var locale in page.Locales)
        {
            model.Locales.Add(new PageLocalizedModel
            {
                LanguageId = locale.LanguageId,
                Title = page.GetTranslation(x => x.Title, locale.LanguageId, false),
                Body = page.GetTranslation(x => x.Body, locale.LanguageId, false)
            });
        }

        return await Task.FromResult(model);
    }

    public virtual async Task<Page> InsertPageModel(PageModel model, string storeId)
    {
        var page = new Page
        {
            Title = model.Title,
            Body = model.Body,
            PageLayoutId = model.PageLayoutId,
            Published = model.Published,
            DisplayOrder = model.DisplayOrder,
            IncludeInSitemap = false,
            IsPasswordProtected = false,
            LimitedToStores = true,
            Stores = new List<string> { storeId }
        };

        // Don't allow setting SystemName from the UI to prevent duplicates
        page.SystemName = null;

        // Handle locales
        foreach (var locale in model.Locales)
        {
            page.Locales.Add(new Domain.Localization.TranslationEntity
            {
                LanguageId = locale.LanguageId,
                LocaleKey = "Title",
                LocaleValue = locale.Title
            });
            page.Locales.Add(new Domain.Localization.TranslationEntity
            {
                LanguageId = locale.LanguageId,
                LocaleKey = "Body",
                LocaleValue = locale.Body
            });
        }

        // Generate SEName
        page.SeName = await _seNameService.ValidateSeName(page, "", page.Title, true);

        await _pageService.InsertPage(page);
        await _seNameService.SaveSeName(page);

        return page;
    }

    public virtual async Task<Page> UpdatePageModel(Page page, PageModel model, string storeId)
    {
        // Prevent editing system pages (pages with SystemName and no store restrictions)
        if (!string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any())
        {
            throw new InvalidOperationException("Cannot edit system pages. Please copy the page first.");
        }

        // Ensure the page belongs to the current store
        if (!page.Stores.Contains(storeId))
        {
            throw new InvalidOperationException("Cannot edit a page that doesn't belong to this store.");
        }

        page.Title = model.Title;
        page.Body = model.Body;
        page.PageLayoutId = model.PageLayoutId;
        page.Published = model.Published;
        page.DisplayOrder = model.DisplayOrder;

        // Clear and rebuild locales
        page.Locales.Clear();
        foreach (var locale in model.Locales)
        {
            page.Locales.Add(new Domain.Localization.TranslationEntity
            {
                LanguageId = locale.LanguageId,
                LocaleKey = "Title",
                LocaleValue = locale.Title
            });
            page.Locales.Add(new Domain.Localization.TranslationEntity
            {
                LanguageId = locale.LanguageId,
                LocaleKey = "Body",
                LocaleValue = locale.Body
            });
        }

        page.SeName = await _seNameService.ValidateSeName(page, "", page.Title, true);

        await _pageService.UpdatePage(page);
        await _seNameService.SaveSeName(page);

        return page;
    }

    public virtual async Task<Page> CopyPageModel(string sourcePageId, string storeId)
    {
        var sourcePage = await _pageService.GetPageById(sourcePageId);
        if (sourcePage == null)
        {
            throw new ArgumentException("Source page not found");
        }

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

    public virtual async Task DeletePage(Page page)
    {
        // Prevent deletion of system pages
        if (!string.IsNullOrEmpty(page.SystemName) && !page.Stores.Any())
        {
            throw new InvalidOperationException("Cannot delete system pages.");
        }

        await _pageService.DeletePage(page);
    }
}
