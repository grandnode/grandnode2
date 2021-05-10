using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Courses;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Courses
{
    public class CourseLessonValidator : BaseGrandValidator<CourseLessonModel>
    {
        public CourseLessonValidator(
            IEnumerable<IValidatorConsumer<CourseLessonModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.Name.Required"));
            RuleFor(x => x.CourseId).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.CourseId.Required"));
            RuleFor(x => x.SubjectId).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Lesson.Fields.SubjectId.Required"));
        }
    }
}