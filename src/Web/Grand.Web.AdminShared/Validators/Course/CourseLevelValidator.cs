using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Courses;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Course;

public class CourseLevelValidator : BaseGrandValidator<CourseLevelModel>
{
    public CourseLevelValidator(
        IEnumerable<IValidatorConsumer<CourseLevelModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Level.Fields.Name.Required"));
    }
}