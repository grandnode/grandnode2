using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetStoreQueryHandler : IRequestHandler<GetQuery<StoreDto>, IQueryable<StoreDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetStoreQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<StoreDto>> Handle(GetQuery<StoreDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<StoreDto>(typeof(Domain.Stores.Store).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
