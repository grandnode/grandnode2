using FluentValidation;
using Grand.Module.Api.Extensions;
using Grand.Module.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;

namespace Grand.Module.Api.Validators.Catalog;

public class ProductCollectionValidator : BaseGrandValidator<ProductCollectionDto>
{
    public ProductCollectionValidator(IEnumerable<IValidatorConsumer<ProductCollectionDto>> validators,
        ITranslationService translationService,
        IContextAccessor contextAccessor, ICollectionService collectionService)
        : base(validators)
    {
        var languageId = contextAccessor.WorkingLanguageId();

        RuleFor(x => x).MustAsync(async (x, _, _) =>
        {
            var collection = await collectionService.GetCollectionById(x.CollectionId);
            return collection != null;
        }).WithMessage(translationService.GetResource("Api.Catalog.ProductCollection.Fields.CollectionId.NotExists", languageId));
    }
}