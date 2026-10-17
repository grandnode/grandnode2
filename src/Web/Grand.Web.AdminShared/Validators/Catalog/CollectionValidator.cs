using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class CollectionValidator : BaseGrandValidator<CollectionModel>
{
    public CollectionValidator(
        IEnumerable<IValidatorConsumer<CollectionModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Catalog.Collections.Fields.Name.Required"));
    }
}