using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Models.Blogs;
using System.Collections.Generic;

namespace Grand.Web.Validators.Blogs
{
    public class BlogPostValidator : BaseGrandValidator<AddBlogCommentModel>
    {
        public BlogPostValidator(
            IEnumerable<IValidatorConsumer<AddBlogCommentModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.CommentText).NotEmpty().WithMessage(translationService.GetResource("Blog.Comments.CommentText.Required"));
        }}
}