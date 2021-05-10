using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

namespace Grand.Api.Validators.Catalog
{
    public class ProductPictureValidator : BaseGrandValidator<ProductPictureDto>
    {
        public ProductPictureValidator(IEnumerable<IValidatorConsumer<ProductPictureDto>> validators,
            ITranslationService translationService, IPictureService pictureService)
            : base(validators)
        {
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                var picture = await pictureService.GetPictureById(x.PictureId);
                if (picture == null)
                    return false;
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.ProductPicture.Fields.PictureId.NotExists"));
        }
    }
}
