using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetProductQueryHandler : IRequestHandler<GetQuery<ProductDto>, IQueryable<ProductDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetProductQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }

        public async Task<IQueryable<ProductDto>> Handle(GetQuery<ProductDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<ProductDto>(typeof(Domain.Catalog.Product).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
