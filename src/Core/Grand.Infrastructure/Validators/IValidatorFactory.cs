using FluentValidation;

namespace Grand.Infrastructure.Validators;

public interface IValidatorFactory
{
    IValidator<T> GetValidator<T>();
}