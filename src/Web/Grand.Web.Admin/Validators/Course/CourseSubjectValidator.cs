using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Courses;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Courses
{
    public class CourseSubjectValidator : BaseGrandValidator<CourseSubjectModel>
    {
        public CourseSubjectValidator(
            IEnumerable<IValidatorConsumer<CourseSubjectModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.Name.Required"));
            RuleFor(x => x.CourseId).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Subject.Fields.CourseId.Required"));
        }
    }
}