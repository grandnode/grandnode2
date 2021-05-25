using Grand.Api.DTOs.Shipping;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Shipping
{
    public class GetPickupPointQueryHandler : IRequestHandler<GetQuery<PickupPointDto>, IQueryable<PickupPointDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetPickupPointQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<PickupPointDto>> Handle(GetQuery<PickupPointDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<PickupPointDto>(typeof(Domain.Shipping.PickupPoint).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
        }
    }
}