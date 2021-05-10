using FluentValidation;
using Grand.Api.DTOs.Catalog;
using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Storage.Interfaces;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;

namespace Grand.Api.Validators.Catalog
{
    public class BrandValidator : BaseGrandValidator<BrandDto>
    {
        public BrandValidator(IEnumerable<IValidatorConsumer<BrandDto>> validators,
            ITranslationService translationService, IPictureService pictureService, IBrandService brandService, IBrandLayoutService brandLayoutService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Api.Catalog.Brand.Fields.Name.Required"));
            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.PictureId))
                {
                    var picture = await pictureService.GetPictureById(x.PictureId);
                    if (picture == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Brand.Fields.PictureId.NotExists"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.BrandLayoutId))
                {
                    var layout = await brandLayoutService.GetBrandLayoutById(x.BrandLayoutId);
                    if (layout == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Brand.Fields.BrandLayoutId.NotExists"));

            RuleFor(x => x).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.Id))
                {
                    var brand = await brandService.GetBrandById(x.Id);
                    if (brand == null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Catalog.Brand.Fields.Id.NotExists"));
        }
    }
}
