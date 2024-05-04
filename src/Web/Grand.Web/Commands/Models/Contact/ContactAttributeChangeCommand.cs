using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Web.Common.Models;
using MediatR;

namespace Grand.Web.Commands.Models.Contact;

public class
    ContactAttributeChangeCommand : IRequest<(IList<string> enabledAttributeIds, IList<string> disabledAttributeIds)>
{
    public IList<CustomAttributeModel> Attributes { get; set; }
    public Customer Customer { get; set; }
    public Store Store { get; set; }
}