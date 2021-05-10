using FluentValidation;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using System.Collections.Generic;
using Widgets.Slider.Domain;
using Widgets.Slider.Models;

namespace Widgets.Slider.Validators
{
    public class SliderValidator : BaseGrandValidator<SlideModel>
    {
        public SliderValidator(IEnumerable<IValidatorConsumer<SlideModel>> validators,
            ITranslationService translationService) : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Widgets.Slider.Name.Required"));
            RuleFor(x => x.SliderTypeId == (int)SliderType.Category && string.IsNullOrEmpty(x.CategoryId)).Equal(false).WithMessage(translationService.GetResource("Widgets.Slider.Category.Required"));
            RuleFor(x => x.SliderTypeId == (int)SliderType.Collection && string.IsNullOrEmpty(x.CollectionId)).Equal(false).WithMessage(translationService.GetResource("Widgets.Slider.Collection.Required"));
        }
    }
}