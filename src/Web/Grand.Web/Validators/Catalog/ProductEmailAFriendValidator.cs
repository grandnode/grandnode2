using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Catalog;
using System.Collections.Generic;

namespace Grand.Web.Validators.Catalog
{
    public class ProductEmailAFriendValidator : BaseGrandValidator<ProductEmailAFriendModel>
    {
        public ProductEmailAFriendValidator(
            IEnumerable<IValidatorConsumer<ProductEmailAFriendModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.FriendEmail).NotEmpty().WithMessage(translationService.GetResource("Products.EmailAFriend.FriendEmail.Required"));
            RuleFor(x => x.FriendEmail).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));

            RuleFor(x => x.YourEmailAddress).NotEmpty().WithMessage(translationService.GetResource("Products.EmailAFriend.YourEmailAddress.Required"));
            RuleFor(x => x.YourEmailAddress).EmailAddress().WithMessage(translationService.GetResource("Common.WrongEmail"));
        }}
}