using FluentValidation;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Courses;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Courses
{
    public class CourseValidator : BaseGrandValidator<CourseModel>
    {
        public CourseValidator(
            IEnumerable<IValidatorConsumer<CourseModel>> validators,
            ITranslationService translationService, IProductCourseService productCourseService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Courses.Course.Fields.Name.Required"));
            RuleFor(x => x.ProductId).MustAsync(async (x, y, context) =>
            {
                if (!string.IsNullOrEmpty(x.ProductId) && !string.IsNullOrEmpty(x.Id))
                {
                    var course = await productCourseService.GetCourseByProductId(x.ProductId);
                    if (course != null && course.Id != x.Id)
                        return false;
                }
                if (!string.IsNullOrEmpty(x.ProductId) && string.IsNullOrEmpty(x.Id))
                {
                    var course = await productCourseService.GetCourseByProductId(x.ProductId);
                    if (course != null)
                        return false;
                }
                return true;
            }).WithMessage(translationService.GetResource("Admin.Courses.Course.Fields.ProductId.Assigned"));
        }
    }
}