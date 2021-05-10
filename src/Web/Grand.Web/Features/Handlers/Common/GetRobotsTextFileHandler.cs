using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Web.Features.Models.Common;
using MediatR;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Common
{
    public class GetRobotsTextFileHandler : IRequestHandler<GetRobotsTextFile, string>
    {

        private readonly IWorkContext _workContext;
        private readonly ILanguageService _languageService;
        private readonly IMediaFileStore _mediaFileStore;
        private readonly AppConfig _config;

        public GetRobotsTextFileHandler(
            IWorkContext workContext,
            ILanguageService languageService,
            IMediaFileStore mediaFileStore,
            AppConfig config)
        {
            _workContext = workContext;
            _languageService = languageService;
            _mediaFileStore = mediaFileStore;
            _config = config;
        }

        public async Task<string> Handle(GetRobotsTextFile request, CancellationToken cancellationToken)
        {
            return await PrepareRobotsTextFile();
        }

        private async Task<string> PrepareRobotsTextFile()
        {
            var sb = new StringBuilder();

            //if robots.txt exists, use it
            var robotsFile = await _mediaFileStore.GetFileInfo("robots.custom.txt");
            if (robotsFile != null)
            {
                //the robots.txt file exists
                string robotsFileContent = await _mediaFileStore.ReadAllText(robotsFile.Name);
                sb.Append(robotsFileContent);
            }
            else
            {
                //doesn't exist. generate 
                var disallowPaths = new List<string>
                {
                    "/admin",
                    "/bin/",
                    "/assets/files/",
                    "/assets/files/exportimport/",
                    "/country/getstatesbycountryid",
                    "/install",
                    "/setproductreviewhelpfulness",
                };
                var localizableDisallowPaths = new List<string>
                {
                    "/addproducttocart/catalog/",
                    "/addproducttocart/details/",
                    "/outofstocksubscriptions/manage",
                    "/cart",
                    "/changelanguage/*",
                    "/checkout",
                    "/checkout/billingaddress",
                    "/checkout/completed",
                    "/checkout/confirm",
                    "/checkout/shippingaddress",
                    "/checkout/shippingmethod",
                    "/checkout/paymentinfo",
                    "/checkout/paymentmethod",
                    "/clearcomparelist",
                    "/compareproducts",
                    "/compareproducts/add/*",
                    "/account/activation",
                    "/account/addresses",
                    "/account/changepassword",
                    "/account/checkusernameavailability",
                    "/account/downloadableproducts",
                    "/account/info",
                    "/account/auctions",
                    "/common/customeractioneventurl",
                    "/common/getactivepopup",
                    "/common/removepopup",
                    "/deletepm",
                    "/emailwishlist",
                    "/login/*",
                    "/newsletter/subscriptionactivation",
                    "/checkout",
                    "/order/history",
                    "/orderdetails",
                    "/passwordrecovery/confirm",
                    "/popupinteractiveform",
                    "/register/*",
                    "/merchandisereturn",
                    "/merchandisereturn/history",
                    "/loyaltypoints/history",
                    "/search?",
                    "/shoppingcart/*",
                    "/storeclosed",
                    "/subscribenewsletter",
                    "/subscribenewsletter/SaveCategories",
                    "/page/authenticate",
                    "/uploadfileproductattribute",
                    "/uploadfilecheckoutattribute",
                    "/wishlist",
                };


                const string newLine = "\r\n"; //Environment.NewLine
                sb.Append("User-agent: *");
                sb.Append(newLine);
                //sitemaps
                if (_config.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    //URLs are localizable. Append SEO code
                    foreach (var language in await _languageService.GetAllLanguages(storeId: _workContext.CurrentStore.Id))
                    {
                        sb.AppendFormat("Sitemap: {0}{1}/sitemap.xml", _workContext.CurrentStore.Url, language.UniqueSeoCode);
                        sb.Append(newLine);
                    }
                }
                else
                {
                    //localizable paths (without SEO code)
                    sb.AppendFormat("Sitemap: {0}sitemap.xml", _workContext.CurrentStore.Url);
                    sb.Append(newLine);
                }
                //host
                var storeLocation = _workContext.CurrentStore.SslEnabled ? _workContext.CurrentStore.SecureUrl.TrimEnd('/') : _workContext.CurrentStore.Url.TrimEnd('/');
                sb.AppendFormat("Host: {0}", storeLocation);
                sb.Append(newLine);

                //usual paths
                foreach (var path in disallowPaths)
                {
                    sb.AppendFormat("Disallow: {0}", path);
                    sb.Append(newLine);
                }
                //localizable paths (without SEO code)
                foreach (var path in localizableDisallowPaths)
                {
                    sb.AppendFormat("Disallow: {0}", path);
                    sb.Append(newLine);
                }
                if (_config.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    //URLs are localizable. Append SEO code
                    foreach (var language in await _languageService.GetAllLanguages(storeId: _workContext.CurrentStore.Id))
                    {
                        foreach (var path in localizableDisallowPaths)
                        {
                            sb.AppendFormat("Disallow: /{0}{1}", language.UniqueSeoCode, path);
                            sb.Append(newLine);
                        }
                    }
                }

                //load and add robots.txt additions to the end of file.
                var robotsAdditionsFile = await _mediaFileStore.GetFileInfo("robots.additions.txt");
                if (robotsAdditionsFile != null)
                {
                    string robotsFileContent = await _mediaFileStore.ReadAllText(robotsAdditionsFile.Name);
                    sb.Append(robotsFileContent);
                }

            }
            return sb.ToString();
        }

    }
}
