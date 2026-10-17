using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.SharedKernel.Captcha;
using Grand.Web.Models.Catalog;
using Grand.Web.Validators.Catalog;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Grand.Web.Common.Localization;

namespace Grand.Web.Tests.Validators.Catalog;

[TestClass]
public class ProductEmailAFriendValidatorTests
{
    [TestMethod]
    public async Task Validate_UnpublishedProduct_UsesProductDisabledTranslation()
    {
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(translation => translation.GetResource(It.IsAny<string>()))
            .Returns<string>(key => key);
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(context => context.CurrentCustomer).Returns(new Customer());
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(context => context.WorkContext).Returns(workContext.Object);
        var groupService = new Mock<IGroupService>();
        groupService.Setup(group => group.IsGuest(It.IsAny<Customer>())).ReturnsAsync(false);
        var productService = new Mock<IProductService>();
        productService.Setup(product => product.GetProductById("product-id", false))
            .ReturnsAsync((Product)null);

        var validator = new ProductEmailAFriendValidator(
            new List<IValidatorConsumer<ProductEmailAFriendModel>>(),
            new List<IValidatorConsumer<ICaptchaValidModel>>(),
            new CaptchaSettings(),
            new CatalogSettings { EmailAFriendEnabled = true, AllowAnonymousUsersToEmailAFriend = true },
            contextAccessor.Object,
            groupService.Object,
            productService.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IGoogleReCaptchaValidator>().Object,
            translationService.Object);

        var result = await validator.ValidateAsync(new ProductEmailAFriendModel
        {
            ProductId = "product-id",
            FriendEmail = "friend@example.com",
            YourEmailAddress = "sender@example.com"
        });

        Assert.IsTrue(result.Errors.Any(error =>
            error.ErrorMessage == "Products.EmailAFriend.ProductIsDisabled"));
        translationService.Verify(translation =>
            translation.GetResource("Products.EmailAFriend.ProductIsDisabled"), Times.Once);
    }
}
