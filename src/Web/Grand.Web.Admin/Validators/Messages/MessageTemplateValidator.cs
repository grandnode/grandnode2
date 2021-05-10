using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Messages;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Messages
{
    public class MessageTemplateValidator : BaseGrandValidator<MessageTemplateModel>
    {
        public MessageTemplateValidator(
            IEnumerable<IValidatorConsumer<MessageTemplateModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Subject).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Subject.Required"));
            RuleFor(x => x.Body).NotEmpty().WithMessage(translationService.GetResource("Admin.Content.MessageTemplates.Fields.Body.Required"));
        }
    }
}