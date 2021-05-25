using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetStateProvinceQueryHandler : IRequestHandler<GetQuery<StateProvinceDto>, IQueryable<StateProvinceDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetStateProvinceQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<StateProvinceDto>> Handle(GetQuery<StateProvinceDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<StateProvinceDto>(typeof(Domain.Directory.StateProvince).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
