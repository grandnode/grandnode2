using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetVendorByIdQuery : IRequest<Vendor>
    {
        public string Id { get; set; }
    }
}
