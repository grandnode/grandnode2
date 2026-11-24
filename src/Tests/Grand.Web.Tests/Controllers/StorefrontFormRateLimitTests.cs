using Grand.Web.Common.Startup;
using Grand.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace Grand.Web.Tests.Controllers;

/// <summary>
///     The storefront forms that mail the store or keep what a visitor sent, uploads included
/// </summary>
[TestClass]
public class StorefrontFormRateLimitTests
{
    public static IEnumerable<object[]> Forms =>
    [
        [typeof(ContactController), nameof(ContactController.Index)],
        [typeof(ContactController), nameof(ContactController.UploadFileContactAttribute)],
        [typeof(VendorController), nameof(VendorController.ContactVendor)],
        [typeof(ProductController), nameof(ProductController.ProductEmailAFriend)],
        [typeof(ProductController), nameof(ProductController.AskQuestionOnProduct)],
        [typeof(ProductController), nameof(ProductController.ProductReviews)],
        [typeof(ProductController), nameof(ProductController.UploadFileProductAttribute)],
        [typeof(CatalogController), nameof(CatalogController.VendorReviews)],
        [typeof(BlogController), nameof(BlogController.BlogPost)],
        [typeof(NewsController), nameof(NewsController.NewsCommentAdd)],
        [typeof(NewsletterController), nameof(NewsletterController.SubscribeNewsletter)],
        [typeof(AccountController), nameof(AccountController.PasswordRecovery)],
        [typeof(ShoppingCartController), nameof(ShoppingCartController.UploadFileCheckoutAttribute)]
    ];

    [TestMethod]
    [DynamicData(nameof(Forms))]
    public void PostOfTheForm_RateLimited(Type controller, string action)
    {
        var post = controller.GetMethods()
            .Single(m => m.Name == action && m.GetCustomAttribute<HttpPostAttribute>() != null);

        Assert.AreEqual(GrandCommonStartup.StorefrontFormRateLimitPolicy,
            post.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName);
    }
}