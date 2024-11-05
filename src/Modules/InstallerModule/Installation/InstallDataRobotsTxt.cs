using Grand.Domain.Common;
using Grand.Domain.Stores;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual async Task InstallDataRobotsTxt(
        Store store)
    {
        var url = store.SslEnabled ? store.SecureUrl : store.Url;

        var robotsTxt = new RobotsTxt {
            Name = "RobotsTXT",
            StoreId = store.Id,
            Text = @$"User-agent: *
Sitemap: {url}sitemap.xml
Host: {url}
Disallow: /admin
Disallow: /bin/
Disallow: /assets/files/
Disallow: /assets/files/exportimport/
Disallow: /country/getstatesbycountryid
Disallow: /install
Disallow: /setproductreviewhelpfulness
Disallow: /addproducttocart/catalog/
Disallow: /addproducttocart/details/
Disallow: /outofstocksubscriptions/manage
Disallow: /cart
Disallow: /changelanguage/*
Disallow: /changecurrency/*
Disallow: /checkout/*
Disallow: /compareproducts
Disallow: /account/*
Disallow: /emailwishlist
Disallow: /login/*
Disallow: /newsletter/subscriptionactivation
Disallow: /order/*
Disallow: /orderdetails
Disallow: /passwordrecovery/confirm
Disallow: /register/*
Disallow: /merchandisereturn
Disallow: /merchandisereturn/history
Disallow: /loyaltypoints/history
Disallow: /search?
Disallow: /shoppingcart/*
Disallow: /storeclosed
Disallow: /subscribenewsletter/*
Disallow: /page/authenticate
Disallow: /uploadfileproductattribute
Disallow: /uploadfilecheckoutattribute
Disallow: /wishlist
Disallow: /quickview/*"
        };

        await _robotsTxtRepository.InsertAsync(robotsTxt);
    }
}