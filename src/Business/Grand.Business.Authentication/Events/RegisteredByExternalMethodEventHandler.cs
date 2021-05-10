using Grand.Business.Authentication.Utilities;
using Grand.Business.Customers.Utilities;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Authentication.Events
{
    /// <summary>
    /// Automatic customer registration by external authentication method event
    /// </summary>
    public class RegisteredByExternalMethodEventHandler : INotification
    {
        public RegisteredByExternalMethodEventHandler(
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
