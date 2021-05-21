using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetBrandQueryHandler : IRequestHandler<GetQuery<BrandDto>, IQueryable<BrandDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetBrandQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<BrandDto>> Handle(GetQuery<BrandDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<BrandDto>(typeof(Domain.Catalog.Brand).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
