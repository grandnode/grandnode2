using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetCurrencyQueryHandler : IRequestHandler<GetQuery<CurrencyDto>, IQueryable<CurrencyDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetCurrencyQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<CurrencyDto>> Handle(GetQuery<CurrencyDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<CurrencyDto>(typeof(Domain.Directory.Currency).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
        }
    }
}
