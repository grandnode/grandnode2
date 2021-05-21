using Grand.Api.DTOs.Shipping;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Shipping
{
    public class GetDeliveryDateQueryHandler : IRequestHandler<GetQuery<DeliveryDateDto>, IQueryable<DeliveryDateDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetDeliveryDateQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<DeliveryDateDto>> Handle(GetQuery<DeliveryDateDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<DeliveryDateDto>(typeof(Domain.Shipping.DeliveryDate).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
        }
    }
}