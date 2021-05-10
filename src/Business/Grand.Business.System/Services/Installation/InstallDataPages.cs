using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Pages;
using Grand.Domain.Seo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallPages()
        {
            var defaultPageLayout =
                _pageLayoutRepository.Table.FirstOrDefault(tt => tt.Name == "Default layout");
            if (defaultPageLayout == null)
                throw new Exception("Page layout cannot be loaded");

            var pages = new List<Page>
                               {
                                   new Page
                                       {
                                           SystemName = "AboutUs",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           IncludeInFooterRow1 = true,
                                           DisplayOrder = 20,
                                           Title = "About us",
                                           Body = "<p>Put your &quot;About Us&quot; information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "CheckoutAsGuestOrRegister",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p><strong>Register and save time!</strong><br />Register with us for future convenience:</p><ul><li>Fast and easy check out</li><li>Easy access to your order history and status</li></ul>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "ConditionsOfUse",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           IncludeInFooterRow1 = true,
                                           DisplayOrder = 15,
                                           Title = "Conditions of Use",
                                           Body = "<p>Put your conditions of use information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "ContactUs",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p>Put your contact information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "HomePageText",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "Welcome to our store",
                                           Body = "<p>Online shopping is the process consumers go through to purchase products or services over the Internet. You can edit this in the admin site.</p><p>If you have questions, see the <a href=\"https://grandnode.com/\">Documentation</a>, or post in the <a href=\"http://www.grandnode.com/boards/\">Forums</a> at <a href=\"http://www.grandnode.com\">grandnode.com</a></p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "LoginRegistrationInfo",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "About login / registration",
                                           Body = "<p>Put your login / registration information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "PrivacyInfo",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           IncludeInFooterRow1 = true,
                                           DisplayOrder = 10,
                                           Title = "Privacy notice",
                                           Body = "<p>Put your privacy policy information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "AccessDenied",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p><strong>Access to the page is denied .</strong></p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "PageNotFound",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p><strong>The page you requested was not found, and we have a fine guess why.</strong></p><ul><li>If you typed the URL directly, please make sure the spelling is correct.</li><li>The page longer exists. In this case, we profusely apologize for the inconvenience and for any damage this may cause.</li></ul>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "ShippingInfo",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           IncludeInFooterRow1 = true,
                                           DisplayOrder = 5,
                                           Title = "Shipping & returns",
                                           Body = "<p>Put your shipping &amp; returns information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "ApplyVendor",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p>Provide information about the application process for creating a seller (vendor) account. You can edit this in the admin panel.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "VendorTermsOfService",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p>Put your terms of service information here. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                                   new Page
                                       {
                                           SystemName = "KnowledgebaseHomePage",
                                           IncludeInSitemap = false,
                                           IsPasswordProtected = false,
                                           DisplayOrder = 1,
                                           Title = "",
                                           Body = "<p>Knowledgebase homepage. You can edit this in the admin site.</p>",
                                           PageLayoutId = defaultPageLayout.Id,
                                           Published = true
                                       },
                               };
            await _pageRepository.InsertAsync(pages);

            var lpages = from p in _pageRepository.Table
                         select p;
            //search engine names
            foreach (var page in lpages)
            {
                var seName = page.SystemName.ToLowerInvariant();
                await _entityUrlRepository.InsertAsync(new EntityUrl
                {
                    EntityId = page.Id,
                    EntityName = "Page",
                    LanguageId = "",
                    IsActive = true,
                    Slug = seName
                });
                page.SeName = seName;
                await _pageRepository.UpdateAsync(page);
            }

        }

    }
}
