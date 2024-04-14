using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Validators.Course;

public class CourseLevelValidator : BaseGrandValidator<CourseLevelModel>
{
    public CourseLevelValidator(
        IEnumerable<IValidatorConsumer<CourseLevelModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Level.Fields.Name.Required"));
    }
}