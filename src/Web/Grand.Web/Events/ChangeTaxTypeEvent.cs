using Grand.Domain.Customers;
using Grand.Domain.Tax;
using MediatR;

namespace Grand.Web.Events
{
    public class ChangeTaxTypeEvent : INotification
    {
        public Customer Customer { get; private set; }
        public TaxDisplayType TaxDisplayType { get; private set; }

        public ChangeTaxTypeEvent(Customer customer, TaxDisplayType taxDisplayType)
        {
            Customer = customer;
            TaxDisplayType = taxDisplayType;
        }
    }
}
