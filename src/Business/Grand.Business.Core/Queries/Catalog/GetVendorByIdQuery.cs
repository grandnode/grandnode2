using Grand.Domain.Vendors;
using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

public class GetVendorByIdQuery : IRequest<Vendor>
{
    public string Id { get; set; }
}