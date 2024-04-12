using FluentValidation;

namespace Grand.Infrastructure.Tests.Validators;

public class SourceTestValidator : AbstractValidator<SourceTest>
{
    public SourceTestValidator()
    {
        RuleFor(person => person.LastName).NotNull();
    }
}