using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Events.Marketing
{
    /// <summary>
    /// Customer coordinates save - event
    /// </summary>
    public class CustomerCoordinatesEvent : INotification
    {
        public CustomerCoordinatesEvent(Customer customer)
        {
            Customer = customer;
        }

        /// <summary>
        /// Customer
        /// </summary>
        public Customer Customer {
            get; private set;
        }

    }
}
