using Grand.Domain.Data;
using Grand.Domain.Vendors;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Queries.Models.Handlers
{
    public class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, Vendor>
    {
        private readonly IRepository<Vendor> _vendorRepository;

        public GetVendorByIdQueryHandler(IRepository<Vendor> vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<Vendor> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
                return null;

            return await _vendorRepository.GetByIdAsync(request.Id);
        }
    }
}
