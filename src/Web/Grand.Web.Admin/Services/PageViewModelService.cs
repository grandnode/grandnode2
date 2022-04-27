using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Logging;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Domain.Seo;
using Grand.Domain.Pages;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Pages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Admin.Services
{
    public partial class PageViewModelService : IPageViewModelService
    {
        private readonly IPageLayoutService _pageLayoutService;
        private readonly IPageService _pageService;
        private readonly ISlugService _slugService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IStoreService _storeService;
        private readonly ILanguageService _languageService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IWorkContext _workContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SeoSettings _seoSettings;

        public PageViewModelService(
            IPageLayoutService pageLayoutService, 
            IPageService pageService, 
            ISlugService slugService, 
            ITranslationService translationService,
            ICustomerActivityService customerActivityService, 
            IStoreService storeService, 
            ILanguageService languageService, 
            IDateTimeService dateTimeService,
            IWorkContext workContext,
            IHttpContextAccessor httpContextAccessor,
            SeoSettings seoSettings)
        {
            _pageLayoutService = pageLayoutService;
            _pageService = pageService;
            _slugService = slugService;
            _translationService = translationService;
            _customerActivityService = customerActivityService;
            _storeService = storeService;
            _languageService = languageService;
            _dateTimeService = dateTimeService;
            _workContext = workContext;
            _httpContextAccessor = httpContextAccessor;
            _seoSettings = seoSettings;
        }

        public virtual async Task<PageListModel> PreparePageListModel()
        {
            var model = new PageListModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });
            return model;
        }

        public virtual async Task PrepareLayoutsModel(PageModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var layouts = await _pageLayoutService.GetAllPageLayouts();
            foreach (var layout in layouts)
            {
                model.AvailablePageLayouts.Add(new SelectListItem
                {
                    Text = layout.Name,
                    Value = layout.Id
                });
            }
        }
        public virtual async Task<Page> InsertPageModel(PageModel model)
        {
            if (!model.IsPasswordProtected)
            {
                model.Password = null;
            }

            var page = model.ToEntity(_dateTimeService);
            await _pageService.InsertPage(page);
            //search engine name
            model.SeName = await page.ValidateSeName(model.SeName, page.Title ?? page.SystemName, true, _seoSettings, _slugService, _languageService);
            page.Locales = await model.Locales.ToTranslationProperty(page, x => x.Title, _seoSettings, _slugService, _languageService);
            page.SeName = model.SeName;
            await _pageService.UpdatePage(page);
            await _slugService.SaveSlug(page, model.SeName, "");

            //activity log
            _ = _customerActivityService.InsertActivity("AddNewPage", page.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.AddNewPage"), page.Title ?? page.SystemName);
            return page;
        }
        public virtual async Task<Page> UpdatePageModel(Page page, PageModel model)
        {
            if (!model.IsPasswordProtected)
            {
                model.Password = null;
            }
            page = model.ToEntity(page, _dateTimeService);
            page.Locales = await model.Locales.ToTranslationProperty(page, x => x.Title, _seoSettings, _slugService, _languageService);
            model.SeName = await page.ValidateSeName(model.SeName, page.Title ?? page.SystemName, true, _seoSettings, _slugService, _languageService);
            page.SeName = model.SeName;
            await _pageService.UpdatePage(page);

            //search engine name
            await _slugService.SaveSlug(page, model.SeName, "");
            //activity log
            _ = _customerActivityService.InsertActivity("EditPage", page.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.EditPage"), page.Title ?? page.SystemName);

            return page;
        }
        public virtual async Task DeletePage(Page page)
        {
            await _pageService.DeletePage(page);
            //activity log
            _ = _customerActivityService.InsertActivity("DeletePage", page.Id,
                _workContext.CurrentCustomer, _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                _translationService.GetResource("ActivityLog.DeletePage"), page.Title ?? page.SystemName);
        }
    }
}
