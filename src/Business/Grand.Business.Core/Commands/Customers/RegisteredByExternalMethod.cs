using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Commands.Customers
{
    /// <summary>
    /// Automatic customer registration by external authentication method event
    /// </summary>
    public class RegisteredByExternalMethod : INotification
    {
        public RegisteredByExternalMethod(
            Customer customer,
            ExternalAuthParam parameters,
            RegistrationResult registrationResult)
        {
            Customer = customer;
            AuthenticationParameters = parameters;
            RegistrationResult = registrationResult;
        }

        /// <summary>
        /// Gets or sets specified customer
        /// </summary>
        public Customer Customer { get; private set; }

        /// <summary>
        /// Gets or sets parameters of external authentication
        /// </summary>
        public ExternalAuthParam AuthenticationParameters { get; private set; }

        /// <summary>
        /// Gets or sets of registration result
        /// </summary>
        public RegistrationResult RegistrationResult { get; private set; }
    }
}
