using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Validators.Course;

public class CourseSubjectValidator : BaseGrandValidator<CourseSubjectModel>
{
    public CourseSubjectValidator(
        IEnumerable<IValidatorConsumer<CourseSubjectModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.Name.Required"));
        RuleFor(x => x.CourseId).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.CourseId.Required"));
    }
}