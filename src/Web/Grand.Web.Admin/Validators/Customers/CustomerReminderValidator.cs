using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Models.Customers;
using System.Collections.Generic;

namespace Grand.Web.Admin.Validators.Customers
{
    public class CustomerReminderValidator : BaseGrandValidator<CustomerReminderModel>
    {
        public CustomerReminderValidator(
            IEnumerable<IValidatorConsumer<CustomerReminderModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerReminder.Fields.Name.Required"));
        }
    }
    public class CustomerReminderLevelValidator : BaseGrandValidator<CustomerReminderModel.ReminderLevelModel>
    {
        public CustomerReminderLevelValidator(
            IEnumerable<IValidatorConsumer<CustomerReminderModel.ReminderLevelModel>> validators,
            ITranslationService translationService)
            : base(validators)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerReminder.Level.Fields.Name.Required"));
            RuleFor(x => x.Subject).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerReminder.Level.Fields.Subject.Required"));
            RuleFor(x => x.Body).NotEmpty().WithMessage(translationService.GetResource("Admin.Customers.CustomerReminder.Level.Fields.Body.Required"));
            RuleFor(x => x.Hour+x.Day+ x.Minutes).NotEqual(0).WithMessage(translationService.GetResource("Admin.Customers.CustomerReminder.Level.Fields.DayHourMin.Required"));
        }
    }
}