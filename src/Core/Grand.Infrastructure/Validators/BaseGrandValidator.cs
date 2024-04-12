using FluentValidation;

namespace Grand.Infrastructure.Validators;

public abstract class BaseGrandValidator<T> : AbstractValidator<T> where T : class
{
    protected BaseGrandValidator(IEnumerable<IValidatorConsumer<T>> validators)
    {
        PostInitialize(validators);
    }

    private void PostInitialize(IEnumerable<IValidatorConsumer<T>> validators)
    {
        foreach (var item in validators) item.AddRules(this);
    }
}