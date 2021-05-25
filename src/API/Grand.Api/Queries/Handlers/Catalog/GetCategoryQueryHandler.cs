using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetCategoryQueryHandler : IRequestHandler<GetQuery<CategoryDto>, IQueryable<CategoryDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetCategoryQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<CategoryDto>> Handle(GetQuery<CategoryDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<CategoryDto>(typeof(Domain.Catalog.Category).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
