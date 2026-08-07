using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Mediator;

namespace Grand.Web.Events;

public class ChangeLanguageEvent : INotification
{
    public ChangeLanguageEvent(Customer customer, Language language)
    {
        Customer = customer;
        Language = language;
    }

    public Customer Customer { get; private set; }
    public Language Language { get; private set; }
}