using Grand.Business.Core.Utilities.Customers;
using MediatR;

namespace Grand.Business.Core.Events.Customers
{
    public class CustomerRegistrationEvent<C, R> : INotification where C : RegistrationResult where R : RegistrationRequest
    {
        private readonly C _result;
        private readonly R _request;

        public CustomerRegistrationEvent(C result, R request)
        {
            _result = result;
            _request = request;
        }
        public C Result { get { return _result; } }
        public R Request { get { return _request; } }

    }
}
