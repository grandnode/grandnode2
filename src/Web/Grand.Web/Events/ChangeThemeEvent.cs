using Grand.Domain.Customers;
using MediatR;

namespace Grand.Web.Events
{
    public class ChangeThemeEvent : INotification
    {
        public Customer Customer { get; private set; }
        public string ThemeName { get; private set; }

        public ChangeThemeEvent(Customer customer, string themeName)
        {
            Customer = customer;
            ThemeName = themeName;
        }
    }
}
