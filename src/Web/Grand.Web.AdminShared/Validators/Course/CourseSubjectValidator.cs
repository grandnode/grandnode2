using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Courses;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Course;

public class CourseSubjectValidator : BaseGrandValidator<CourseSubjectModel>
{
    public CourseSubjectValidator(
        IEnumerable<IValidatorConsumer<CourseSubjectModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.Name.Required"));
        RuleFor(x => x.CourseId).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.CourseId.Required"));
    }
}