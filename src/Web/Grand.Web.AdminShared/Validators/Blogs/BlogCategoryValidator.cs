using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Blogs;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Blogs;

public class BlogCategoryValidator : BaseGrandValidator<BlogCategoryModel>
{
    public BlogCategoryValidator(IEnumerable<IValidatorConsumer<BlogCategoryModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.Blog.BlogCategory.Fields.Name.Required"));
    }
}