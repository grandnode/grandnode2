using FluentValidation;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Tasks;

namespace Grand.Web.AdminShared.Validators.Tasks;

public class ScheduleTaskValidator : BaseGrandValidator<ScheduleTaskModel>
{
    public ScheduleTaskValidator(
        IEnumerable<IValidatorConsumer<ScheduleTaskModel>> validators)
        : base(validators)
    {
        RuleFor(x => x.TimeInterval).GreaterThan(0).WithMessage("Time interval must be greater than zero");
    }
}