using Grand.Domain.Customers;
using Grand.Web.Common.Models;
using Grand.Mediator;

namespace Grand.Web.Commands.Models.Contact;

public class
    ContactAttributeChangeCommand : IRequest<(IList<string> enabledAttributeIds, IList<string> disabledAttributeIds)>
{
    public IList<CustomAttributeModel>? Attributes { get; set; }
    public required Customer Customer { get; set; }
    public required Domain.Stores.Store Store { get; set; }
}