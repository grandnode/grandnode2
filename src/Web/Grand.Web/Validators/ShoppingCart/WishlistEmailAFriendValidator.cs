using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.ShoppingCart;
using System.Collections.Generic;

namespace Grand.Web.Validators.ShoppingCart
{
    public class WishlistEmailAFriendValidator : BaseGrandValidator<WishlistEmailAFriendModel>
    {
        public WishlistEmailAFriendValidator(
            IEnumerable<IValidatorConsumer<WishlistEmailAFriendModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.FriendEmail).NotEmpty().WithMessage(translationService.GetResource("Wishlist.EmailAFriend.FriendEmail.Required"));
            RuleFor(x => x.FriendEmail).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

            RuleFor(x => x.YourEmailAddress).NotEmpty().WithMessage(translationService.GetResource("Wishlist.EmailAFriend.YourEmailAddress.Required"));
            RuleFor(x => x.YourEmailAddress).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        }}
}