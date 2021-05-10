using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Messages.Queries.Models
{
    public class GetVendorByIdQuery : IRequest<Vendor>
    {
        public string Id { get; set; }
    }
}
