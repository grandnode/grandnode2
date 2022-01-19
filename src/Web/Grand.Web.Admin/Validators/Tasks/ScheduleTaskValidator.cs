using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Tasks;

namespace Grand.Web.Admin.Validators.Tasks
{
    public class ScheduleTaskValidator : BaseGrandValidator<ScheduleTaskModel>
    {
        public ScheduleTaskValidator(
            IEnumerable<IValidatorConsumer<ScheduleTaskModel>> validators)
            : base(validators)
        {
            RuleFor(x => x.TimeInterval).GreaterThan(0).WithMessage("Time interval must be greater than zero");
        }
    }
}