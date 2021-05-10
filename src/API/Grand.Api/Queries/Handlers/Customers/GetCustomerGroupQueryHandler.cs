using Grand.Api.DTOs.Customers;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Customers
{
    public class GetCustomerGroupQueryHandler : IRequestHandler<GetQuery<CustomerGroupDto>, IMongoQueryable<CustomerGroupDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetCustomerGroupQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public Task<IMongoQueryable<CustomerGroupDto>> Handle(GetQuery<CustomerGroupDto> request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
                return Task.FromResult(
                    _mongoDBContext.Database()
                    .GetCollection<CustomerGroupDto>
                    (typeof(Domain.Customers.CustomerGroup).Name)
                    .AsQueryable());
            else
                return Task.FromResult(_mongoDBContext.Database()
                    .GetCollection<CustomerGroupDto>(typeof(Domain.Customers.CustomerGroup).Name)
                    .AsQueryable()
                    .Where(x => x.Id == request.Id));
        }
    }
}
