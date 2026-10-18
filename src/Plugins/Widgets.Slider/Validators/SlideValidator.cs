using FluentValidation;
using Grand.Infrastructure.Validators;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;
using Grand.Web.Common.Localization;

namespace Widgets.Slider.Validators;

public class SliderValidator : BaseGrandValidator<SlideModel>
{
    public SliderValidator(IEnumerable<IValidatorConsumer<SlideModel>> validators,
        IRequestTranslationService translationService) : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Widgets.Slider.Name.Required"));
        RuleFor(x => x.SliderTypeId == (int)SliderType.Category && string.IsNullOrEmpty(x.CategoryId)).Equal(false)
            .WithMessage(translationService.GetResource("Widgets.Slider.Category.Required"));
        RuleFor(x => x.SliderTypeId == (int)SliderType.Collection && string.IsNullOrEmpty(x.CollectionId)).Equal(false)
            .WithMessage(translationService.GetResource("Widgets.Slider.Collection.Required"));
    }
}