using FluentValidation;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Messages;

namespace Grand.Web.AdminShared.Validators.Messages;

public class MessageTemplateValidator : BaseGrandValidator<MessageTemplateModel>
{
    public MessageTemplateValidator(
        IEnumerable<IValidatorConsumer<MessageTemplateModel>> validators,
        ITranslationService translationService)
        : base(validators)
    {
        RuleFor(x => x.Subject).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Subject.Required"));
        RuleFor(x => x.Body).NotEmpty()
            .WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Body.Required"));
    }
}