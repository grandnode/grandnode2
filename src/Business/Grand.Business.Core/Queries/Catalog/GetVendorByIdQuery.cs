using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog
{
    public class GetVendorByIdQuery : IRequest<Vendor>
    {
        public string Id { get; set; }
    }
}
