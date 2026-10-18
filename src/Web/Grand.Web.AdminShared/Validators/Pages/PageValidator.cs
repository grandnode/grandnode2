using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Pages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Pages;

public class PageValidator : BaseGrandValidator<PageModel>
{
    public PageValidator(
        IEnumerable<IValidatorConsumer<PageModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.SystemName).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.Pages.Fields.SystemName.Required"));
        RuleFor(x => x).Custom((model, context) =>
        {
            if (model.StartDateUtc.HasValue && model.EndDateUtc.HasValue && model.StartDateUtc >= model.EndDateUtc)
                context.AddFailure(nameof(model.StartDateUtc), "Start Date cannot be later than End Date");
        });
    }
}