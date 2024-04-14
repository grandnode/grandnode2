using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Courses;

namespace Grand.Web.Admin.Validators.Course;

public class CourseValidator : BaseGrandValidator<CourseModel>
{
    public CourseValidator(
        IEnumerable<IValidatorConsumer<CourseModel>> validators,
        ITranslationService translationService,
        IProductService productService,
        IProductCourseService productCourseService)
        : base(validators)
    {
        RuleFor(x => x.Name).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Courses.Course.Fields.Name.Required"));
        RuleFor(x => x.ProductId).MustAsync(async (x, _, _) =>
        {
            if (!string.IsNullOrEmpty(x.ProductId))
            {
                var product = await productService.GetProductById(x.ProductId);
                if (product == null)
                    return false;
            }

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