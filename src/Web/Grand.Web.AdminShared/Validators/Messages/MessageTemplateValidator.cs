using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Messages;

public class MessageTemplateValidator : BaseGrandValidator<MessageTemplateModel>
{
    public MessageTemplateValidator(
        IEnumerable<IValidatorConsumer<MessageTemplateModel>> validators,
        IRequestTranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Subject).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Subject.Required"));
        RuleFor(x => x.Body).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Body.Required"));
    }
}