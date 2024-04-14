using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using IValidatorFactory = Grand.Infrastructure.Validators.IValidatorFactory;

namespace Grand.Web.Common.Validators;

public class ValidatorFactory : IValidatorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ValidatorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IValidator<T> GetValidator<T>()
    {
        var validator = _serviceProvider.GetService<IValidator<T>>();
        if (validator != null) return validator;
        throw new InvalidOperationException($"No validator found for type {typeof(T).Name}.");
    }
}