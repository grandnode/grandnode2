using Grand.Domain.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Customers;

public class GetNotes : IRequest<CustomerNotesModel>
{
    public required Customer Customer { get; set; }
}