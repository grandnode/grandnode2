using Grand.Business.Core.Utilities.Customers;
using MediatR;

namespace Grand.Business.Core.Events.Customers
{
    public class CustomerRegistrationEvent<R> : INotification where R : RegistrationRequest
    {
        private readonly R _request;

        public CustomerRegistrationEvent(R request)
        {
            _request = request;
        }
        public R Request => _request;
    }
}
