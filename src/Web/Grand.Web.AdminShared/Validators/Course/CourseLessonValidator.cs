using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Courses;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Course;

public class CourseLessonValidator : BaseGrandValidator<CourseLessonModel>
{
    public CourseLessonValidator(
        IEnumerable<IValidatorConsumer<CourseLessonModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.Name.Required"));
        RuleFor(x => x.CourseId).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.CourseId.Required"));
        RuleFor(x => x.SubjectId).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.SubjectId.Required"));
    }
}