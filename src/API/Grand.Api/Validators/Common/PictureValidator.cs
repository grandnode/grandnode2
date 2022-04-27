using FluentValidation;
using Grand.Api.DTOs.Common;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;

namespace Grand.Api.Validators.Common
{
    public class PictureValidator : BaseGrandValidator<PictureDto>
    {
        public PictureValidator(
            IEnumerable<IValidatorConsumer<PictureDto>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.MimeType).NotEmpty().WithMessage(translationService.GetResource("Api.Common.Picture.Fields.MimeType.Required"));
            RuleFor(x => x.PictureBinary).Must((x, context) =>
            {
                if (!string.IsNullOrEmpty(x.Id) && (x.PictureBinary==null || x.PictureBinary.Length == 0))
                {
                    return true;
                }
                if (string.IsNullOrEmpty(x.Id) && (x.PictureBinary == null || x.PictureBinary.Length == 0))
                {
                    return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Api.Common.Picture.Fields.PictureBinary.Required"));
        }
    }
}
