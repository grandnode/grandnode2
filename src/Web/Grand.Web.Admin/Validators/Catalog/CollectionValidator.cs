using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class CollectionValidator : BaseGrandValidator<CollectionModel>
    {
        public CollectionValidator(
            IEnumerable<IValidatorConsumer<CollectionModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Catalog.Collections.Fields.Name.Required"));
        }
    }
}
