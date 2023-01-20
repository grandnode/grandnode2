using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.Models.Blogs;

namespace Grand.Web.Validators.Blogs;

public class AddBlogCommentValidator : BaseGrandValidator<AddBlogCommentModel>
{
    public AddBlogCommentValidator(
        IEnumerable<IValidatorConsumer<AddBlogCommentModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.CommentText).NotEmpty()
            .WithMessage(translationService.GetResource("Blog.Comments.CommentText.Required"));
    }
}