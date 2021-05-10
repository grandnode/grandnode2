using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Blogs;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Blogs
{
    public class BlogPostValidator : BaseGrandValidator<BlogPostModel>
    {
        public BlogPostValidator(
            IEnumerable<IValidatorConsumer<BlogPostModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Content.Blog.BlogPosts.Fields.Title.Required"));

            RuleFor(x => x.Body)
                .NotEmpty()
                .WithMessage(translationService.GetResource("Admin.Content.Blog.BlogPosts.Fields.Body.Required"));

            //blog tags should not contain dots
            //current implementation does not support it because it can be handled as file extension
            RuleFor(x => x.Tags)
                .Must(x => x == null || !x.Contains("."))
                .WithMessage(translationService.GetResource("Admin.Content.Blog.BlogPosts.Fields.Tags.NoDots"));

        }
    }
}