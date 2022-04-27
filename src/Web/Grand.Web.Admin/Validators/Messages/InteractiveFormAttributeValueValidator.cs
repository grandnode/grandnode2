﻿using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Web.Admin.Models.Messages;

namespace Grand.Web.Admin.Validators.Messages
{
    public class InteractiveFormAttributeValueValidator : BaseGrandValidator<InteractiveFormAttributeValueModel>
    {
        public InteractiveFormAttributeValueValidator(
            IEnumerable<IValidatorConsumer<InteractiveFormAttributeValueModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("admin.marketing.InteractiveForms.Attribute.Values.Fields.Name.Required"));
        }
    }
}