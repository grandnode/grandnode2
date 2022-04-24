using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Catalog
{
    public class ProductCollectionValidator : BaseGrandValidator<ProductCollectionDto>
    {
        public ProductCollectionValidator(IEnumerable<IValidatorConsumer<ProductCollectionDto>> validators,
            ITranslationService translationService, ICollectionService collectionService)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                var collection = await collectionService.GetCollectionById(x.CollectionId);
                if (collection == null)
                    return false;
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductCollection.Fields.CollectionId.NotExists"));
        }
    }
}
