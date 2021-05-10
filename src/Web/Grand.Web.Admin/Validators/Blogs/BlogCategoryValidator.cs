using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Blogs;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Blogs
{
    public class BlogCategoryValidator : BaseGrandValidator<BlogCategoryModel>
    {
        public BlogCategoryValidator(IEnumerable<IValidatorConsumer<BlogCategoryModel>> validators, 
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Content.Blog.BlogCategory.Fields.Name.Required"));
        }
    }
}