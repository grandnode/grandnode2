using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetLayoutQueryHandler : IRequestHandler<GetLayoutQuery, IQueryable<LayoutDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetLayoutQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IQueryable<LayoutDto>> Handle(GetLayoutQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<LayoutDto>(request.LayoutName);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
