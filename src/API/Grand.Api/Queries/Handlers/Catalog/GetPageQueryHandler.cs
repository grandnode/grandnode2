using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
	public class GetPageQueryHandler : IRequestHandler<GetQuery<PageDto>, IQueryable<PageDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetPageQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<IQueryable<PageDto>> Handle(GetQuery<PageDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<PageDto>(typeof(Domain.Pages.Page).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
