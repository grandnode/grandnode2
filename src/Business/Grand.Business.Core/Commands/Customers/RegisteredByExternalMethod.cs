using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Commands.Customers;

/// <summary>
///     Automatic customer registration by external authentication method event
/// </summary>
public class RegisteredByExternalMethod : INotification
{
    public RegisteredByExternalMethod(
        Customer customer,
        ExternalAuthParam parameters)
    {
        Customer = customer;
        AuthenticationParameters = parameters;
    }

    /// <summary>
    ///     Gets or sets specified customer
    /// </summary>
    public Customer Customer { get; private set; }

    /// <summary>
    ///     Gets or sets parameters of external authentication
    /// </summary>
    public ExternalAuthParam AuthenticationParameters { get; private set; }
}