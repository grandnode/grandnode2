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
        private readonly IDatabaseContext _dbContext;

        public GetProductQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IQueryable<ProductDto>> Handle(GetQuery<ProductDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<ProductDto>(typeof(Domain.Catalog.Product).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
