using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetProductAttributeQueryHandler : IRequestHandler<GetQuery<ProductAttributeDto>, IQueryable<ProductAttributeDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetProductAttributeQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<ProductAttributeDto>> Handle(GetQuery<ProductAttributeDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<ProductAttributeDto>(typeof(Domain.Catalog.ProductAttribute).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
