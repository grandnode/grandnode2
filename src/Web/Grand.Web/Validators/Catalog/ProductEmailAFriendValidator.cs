using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.Catalog;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.Catalog;

public class ProductEmailAFriendValidator : BaseGrandValidator<ProductEmailAFriendModel>
{
    public ProductEmailAFriendValidator(
        IEnumerable<IValidatorConsumer<ProductEmailAFriendModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        CaptchaSettings captchaSettings, CatalogSettings catalogSettings,
        IWorkContext workContext, IGroupService groupService, IProductService productService,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.FriendEmail).NotEmpty()
            .WithMessage(translationService.GetResource("Products.EmailAFriend.FriendEmail.Required"));
        RuleFor(x => x.FriendEmail).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        RuleFor(x => x.YourEmailAddress).NotEmpty()
            .WithMessage(translationService.GetResource("Products.EmailAFriend.YourEmailAddress.Required"));
        RuleFor(x => x.YourEmailAddress).EmailAddress()
            .WithMessage(translationService.GetResource("Common.WrongEmail"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnEmailProductToFriendPage)
        {
            RuleFor(x => x.Captcha).NotNull().WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            var product = await productService.GetProductById(x.ProductId);
            if (product is not { Published: true } || !catalogSettings.EmailAFriendEnabled)
                context.AddFailure("Product is disabled");

            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !catalogSettings.AllowAnonymousUsersToEmailAFriend)
                context.AddFailure(translationService.GetResource("Products.EmailAFriend.OnlyRegisteredUsers"));
        });
    }
}