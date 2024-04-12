using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain.Vendors;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetVendorByIdQueryHandler : IRequestHandler<GetVendorByIdQuery, Vendor>
{
    private readonly IRepository<Vendor> _vendorRepository;

    public GetVendorByIdQueryHandler(IRepository<Vendor> vendorRepository)
    {
        _vendorRepository = vendorRepository;
    }

    public Task<Vendor> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(request.Id)
            ? Task.FromResult<Vendor>(null)
            : _vendorRepository.GetByIdAsync(request.Id);
    }
}