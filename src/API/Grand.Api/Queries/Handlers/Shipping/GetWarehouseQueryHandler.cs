using Grand.Api.DTOs.Shipping;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Shipping
{
    public class GetWarehouseQueryHandler : IRequestHandler<GetQuery<WarehouseDto>, IQueryable<WarehouseDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetWarehouseQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<WarehouseDto>> Handle(GetQuery<WarehouseDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<WarehouseDto>(typeof(Domain.Shipping.Warehouse).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
        }
    }
}