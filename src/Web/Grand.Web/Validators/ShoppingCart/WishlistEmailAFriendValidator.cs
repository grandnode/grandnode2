using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Infrastructure.Models;
using Grand.Infrastructure.Validators;
using Grand.Web.Common.Security.Captcha;
using Grand.Web.Common.Validators;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Http;

namespace Grand.Web.Validators.ShoppingCart;

public class WishlistEmailAFriendValidator : BaseGrandValidator<WishlistEmailAFriendModel>
{
    public WishlistEmailAFriendValidator(
        IEnumerable<IValidatorConsumer<WishlistEmailAFriendModel>> validators,
        IEnumerable<IValidatorConsumer<ICaptchaValidModel>> validatorsCaptcha,
        IWorkContext workContext, IGroupService groupService,
        CaptchaSettings captchaSettings, ShoppingCartSettings shoppingCartSettings,
        IHttpContextAccessor contextAccessor, GoogleReCaptchaValidator googleReCaptchaValidator,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.FriendEmail).NotEmpty()
            .WithMessage(translationService.GetResource("Wishlist.EmailAFriend.FriendEmail.Required"));
        RuleFor(x => x.FriendEmail).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

        RuleFor(x => x.YourEmailAddress).NotEmpty()
            .WithMessage(translationService.GetResource("Wishlist.EmailAFriend.YourEmailAddress.Required"));
        RuleFor(x => x.YourEmailAddress).EmailAddress()
            .WithMessage(translationService.GetResource("Common.WrongEmail"));

        if (captchaSettings.Enabled && captchaSettings.ShowOnEmailWishlistToFriendPage)
        {
            RuleFor(x => x.Captcha).NotNull()
                .WithMessage(translationService.GetResource("Account.Captcha.Required"));
            RuleFor(x => x.Captcha)
                .SetValidator(new CaptchaValidator(validatorsCaptcha, contextAccessor, googleReCaptchaValidator));
        }

        RuleFor(x => x).CustomAsync(async (x, context, _) =>
        {
            //check whether the current customer is guest and ia allowed to email wishlist
            if (await groupService.IsGuest(workContext.CurrentCustomer) &&
                !shoppingCartSettings.AllowAnonymousUsersToEmailWishlist)
                context.AddFailure(translationService.GetResource("Wishlist.EmailAFriend.OnlyRegisteredUsers"));
        });
    }
}