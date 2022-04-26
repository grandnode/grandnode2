using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Infrastructure.Validators;

namespace Grand.Api.Validators.Catalog
{
    public class CollectionValidator : BaseGrandValidator<CollectionDto>
    {
        public CollectionValidator(IEnumerable<IValidatorConsumer<CollectionDto>> validators,
            ITranslationService translationService, IPictureService pictureService, ICollectionService collectionService, ICollectionLayoutService collectionLayoutService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Api.Catalog.Collection.Fields.Name.Required"));
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.PictureId))
                {
                    var picture = await pictureService.GetPictureById(x.PictureId);
                    if (picture == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Collection.Fields.PictureId.NotExists"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.CollectionLayoutId))
                {
                    var layout = await collectionLayoutService.GetCollectionLayoutById(x.CollectionLayoutId);
                    if (layout == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Collection.Fields.CollectionLayoutId.NotExists"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.Id))
                {
                    var collection = await collectionService.GetCollectionById(x.Id);
                    if (collection == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Collection.Fields.Id.NotExists"));
        }
    }
}
