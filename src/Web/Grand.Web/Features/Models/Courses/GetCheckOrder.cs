using Grand.Domain.Courses;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Courses;

public class GetCheckOrder : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public required Course Course { get; set; }
}