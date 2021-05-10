using Grand.Business.Cms.Interfaces;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Services.Security;
using Grand.Infrastructure;
using Grand.Domain.Blogs;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Knowledgebase;
using Grand.Domain.News;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using Grand.Web.Common.Components;
using Grand.Web.Models.Common;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class FooterViewComponent : BaseViewComponent
    {
        private readonly IWorkContext _workContext;
        private readonly IPageService _pageService;
        private readonly IPermissionService _permissionService;

        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly BlogSettings _blogSettings;
        private readonly KnowledgebaseSettings _knowledgebaseSettings;
        private readonly NewsSettings _newsSettings;
        private readonly VendorSettings _vendorSettings;
        private readonly CommonSettings _commonSettings;

        public FooterViewComponent(
            IWorkContext workContext,
            IPageService pageService,
            IPermissionService permissionService,
            StoreInformationSettings storeInformationSettings,
            CatalogSettings catalogSettings,
            BlogSettings blogSettings,
            KnowledgebaseSettings knowledgebaseSettings,
            NewsSettings newsSettings,
            VendorSettings vendorSettings,
            CommonSettings commonSettings)
        {
            _workContext = workContext;
            _pageService = pageService;
            _permissionService = permissionService;

            _storeInformationSettings = storeInformationSettings;
            _catalogSettings = catalogSettings;
            _blogSettings = blogSettings;
            _knowledgebaseSettings = knowledgebaseSettings;
            _newsSettings = newsSettings;
            _vendorSettings = vendorSettings;
            _commonSettings = commonSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await PrepareFooter();
            return View(model);
        }
        private async Task<FooterModel> PrepareFooter()
        {
            var now = DateTime.UtcNow;
            var pageModel = (await _pageService.GetAllPages(_workContext.CurrentStore.Id))
                .Where(t => (t.IncludeInFooterRow1 || t.IncludeInFooterRow2 || t.IncludeInFooterRow3) && t.Published &&
                            (!t.StartDateUtc.HasValue || t.StartDateUtc < now) && (!t.EndDateUtc.HasValue || t.EndDateUtc > now))
                .Select(t => new FooterModel.FooterPageModel
                {
                    Id = t.Id,
                    Name = t.GetTranslation(x => x.Title, _workContext.WorkingLanguage.Id),
                    SeName = t.GetSeName(_workContext.WorkingLanguage.Id),
                    IncludeInFooterRow1 = t.IncludeInFooterRow1,
                    IncludeInFooterRow2 = t.IncludeInFooterRow2,
                    IncludeInFooterRow3 = t.IncludeInFooterRow3
                }).ToList();

            //model
            var currentstore = _workContext.CurrentStore;
            var model = new FooterModel
            {
                StoreName = currentstore.GetTranslation(x => x.Name, _workContext.WorkingLanguage.Id),
                CompanyName = currentstore.CompanyName,
                CompanyEmail = currentstore.CompanyEmail,
                CompanyAddress = currentstore.CompanyAddress,
                CompanyPhone = currentstore.CompanyPhoneNumber,
                CompanyHours = currentstore.CompanyHours,
                PrivacyPreference = _storeInformationSettings.DisplayPrivacyPreference,
                WishlistEnabled = await _permissionService.Authorize(StandardPermission.EnableWishlist),
                ShoppingCartEnabled = await _permissionService.Authorize(StandardPermission.EnableShoppingCart),
                SitemapEnabled = _commonSettings.SitemapEnabled,
                WorkingLanguageId = _workContext.WorkingLanguage.Id,
                FacebookLink = _storeInformationSettings.FacebookLink,
                TwitterLink = _storeInformationSettings.TwitterLink,
                YoutubeLink = _storeInformationSettings.YoutubeLink,
                InstagramLink = _storeInformationSettings.InstagramLink,
                LinkedInLink = _storeInformationSettings.LinkedInLink,
                PinterestLink = _storeInformationSettings.PinterestLink,
                BlogEnabled = _blogSettings.Enabled,
                KnowledgebaseEnabled = _knowledgebaseSettings.Enabled,
                CompareProductsEnabled = _catalogSettings.CompareProductsEnabled,
                NewsEnabled = _newsSettings.Enabled,
                RecentlyViewedProductsEnabled = _catalogSettings.RecentlyViewedProductsEnabled,
                RecommendedProductsEnabled = _catalogSettings.RecommendedProductsEnabled,
                NewProductsEnabled = _catalogSettings.NewProductsEnabled,
                InclTax = _workContext.TaxDisplayType == TaxDisplayType.IncludingTax,
                AllowCustomersToApplyForVendorAccount = _vendorSettings.AllowCustomersToApplyForVendorAccount,
                Pages = pageModel
            };

            return model;
        }


    }
}