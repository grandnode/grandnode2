using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Customers
{
    public class GetCustomerGroupQueryHandler : IRequestHandler<GetQuery<CustomerGroupDto>, IQueryable<CustomerGroupDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetCustomerGroupQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<CustomerGroupDto>> Handle(GetQuery<CustomerGroupDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<CustomerGroupDto>(typeof(Domain.Customers.CustomerGroup).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
