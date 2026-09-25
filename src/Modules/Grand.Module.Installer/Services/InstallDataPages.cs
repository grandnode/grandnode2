using Grand.Domain.Pages;
using Grand.Domain.Seo;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallPages()
    {
        var defaultPageLayout =
            _pageLayoutRepository.Table.FirstOrDefault(tt => tt.Name == "Default layout");
        if (defaultPageLayout == null)
            throw new Exception("Page layout cannot be loaded");

        var pages = new List<Page> {
            new() {
                SystemName = "AboutUs",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                IncludeInFooterRow1 = true,
                DisplayOrder = 20,
                Title = "About us",
                Body =
                    "<h2>About [Your company name]</h2><p>[Your company name] was founded to [describe what your business does and who it serves]. We believe in [a sentence or two about what your company stands for].</p><h2>What we offer</h2><ul><li>[Product or service highlight]</li><li>[Product or service highlight]</li><li>[Product or service highlight]</li></ul><h2>Get in touch</h2><p>Have a question? Reach us at [support e-mail] or [phone number]. You can edit this page at any time from the admin site.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "CheckoutAsGuestOrRegister",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body =
                    "<p><strong>Register and save time!</strong><br />Register with us for future convenience:</p><ul><li>Fast and easy check out</li><li>Easy access to your order history and status</li></ul>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "ConditionsOfUse",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                IncludeInFooterRow1 = true,
                DisplayOrder = 15,
                Title = "Conditions of Use",
                Body =
                    "<h2>Conditions of Use</h2><p>By using this website, operated by [Your company name], you agree to the following terms.</p><h2>Using our store</h2><ul><li>You must provide accurate information when creating an account or placing an order.</li><li>Prices and availability are subject to change without notice.</li><li>Content on this site may not be copied or reused without permission.</li></ul><h2>Orders and payment</h2><p>We accept the payment methods shown at checkout. Orders are confirmed once payment has been processed successfully.</p><h2>Contact</h2><p>Questions about these conditions can be sent to [support e-mail].</p><p><em>This is a general-purpose template, not legal advice. Have these terms reviewed by a qualified professional before publishing them.</em></p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "ContactUs",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body = "<p>Put your contact information here. You can edit this in the admin site.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "HomePageText",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "Welcome to our store",
                Body =
                    "<p>Welcome to [Your company name]! We're glad you're here. Browse our catalog to find products chosen with care, and let us know if we can help you find exactly what you're looking for.</p><p>Have a question before you order? Contact us at [support e-mail] &mdash; we're happy to help.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "LoginRegistrationInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "About login / registration",
                Body = "<p>Put your login / registration information here. You can edit this in the admin site.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "PrivacyInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                IncludeInFooterRow1 = true,
                DisplayOrder = 10,
                Title = "Privacy notice",
                Body =
                    "<h2>Privacy notice</h2><p>[Your company name] collects the personal information you provide when you create an account, place an order, or contact us, such as your name, address, e-mail, and payment details.</p><h2>How we use your information</h2><ul><li>To process and deliver your orders.</li><li>To respond to your questions and support requests.</li><li>To send you updates, if you have opted in to receive them.</li></ul><h2>Your rights</h2><p>You may request access to, correction of, or deletion of your personal information at any time by contacting [support e-mail].</p><p><em>This is a general-purpose template, not legal advice. Have your privacy policy reviewed by a qualified professional before publishing it, to make sure it meets the requirements that apply to your business and location.</em></p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "AccessDenied",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body = "<p><strong>Access to the page is denied .</strong></p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "PageNotFound",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body =
                    "<p><strong>The page you requested was not found, and we have a fine guess why.</strong></p><ul><li>If you typed the URL directly, please make sure the spelling is correct.</li><li>The page longer exists. In this case, we profusely apologize for the inconvenience and for any damage this may cause.</li></ul>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "ShippingInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                IncludeInFooterRow1 = true,
                DisplayOrder = 5,
                Title = "Shipping & returns",
                Body =
                    "<h2>Shipping</h2><p>We ship to [countries/regions you ship to]. Orders are typically dispatched within [processing time, e.g. 1-2 business days] and delivered within [delivery window, e.g. 3-5 business days].</p><ul><li>Shipping costs are calculated at checkout based on your order and delivery address.</li><li>You will receive a confirmation e-mail with tracking information once your order ships.</li></ul><h2>Returns</h2><p>If you're not satisfied with your purchase, you may return it within [returns window, e.g. 30 days] of delivery. Items must be [condition requirements, e.g. unused and in original packaging].</p><p>To start a return, contact us at [support e-mail].</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "ApplyVendor",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body =
                    "<p>Provide information about the application process for creating a seller (vendor) account. You can edit this in the admin panel.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "VendorTermsOfService",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body = "<p>Put your terms of service information here. You can edit this in the admin site.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "KnowledgebaseHomePage",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "",
                Body = "<p>Knowledgebase homepage. You can edit this in the admin site.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "VendorPortalInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "Welcome to our Vendor Management Hub!",
                Body =
                    "<p>Manage your product catalog, oversee customer orders, and streamline your shipping processes. Your vendor dashboard is the command center for your success. Stay organized, serve your customers efficiently, and watch your business thrive.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                SystemName = "StorePortalInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "Welcome to our Store Management Hub!",
                Body =
                    "<p>Manage your product catalog, oversee customer orders, and streamline your shipping processes. Your store dashboard is the command center for your success. Stay organized, serve your customers efficiently, and watch your business thrive.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = true
            },
            new() {
                //the welcome card of the admin dashboard - not published, so the storefront never serves it
                SystemName = "AdminPortalInfo",
                IncludeInSitemap = false,
                IsPasswordProtected = false,
                DisplayOrder = 1,
                Title = "Welcome to your admin dashboard",
                Body =
                    "<p>Follow today's orders, online visitors and active carts at a glance, see what needs your attention and jump straight into your daily work. You can edit or translate this message under Content &gt; Landing pages.</p>",
                PageLayoutId = defaultPageLayout.Id,
                Published = false
            }

        };
        pages.ForEach(x => _pageRepository.Insert(x));

        var lpages = from p in _pageRepository.Table
            select p;
        //search engine names
        foreach (var page in lpages)
        {
            var seName = page.SystemName.ToLowerInvariant();
            await _entityUrlRepository.InsertAsync(new EntityUrl {
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