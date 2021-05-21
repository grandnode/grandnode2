using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Customers
{
    public class GetVendorQueryHandler : IRequestHandler<GetQuery<VendorDto>, IQueryable<VendorDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetVendorQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<VendorDto>> Handle(GetQuery<VendorDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<VendorDto>(typeof(Domain.Vendors.Vendor).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
