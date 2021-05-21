using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetCountryQueryHandler : IRequestHandler<GetQuery<CountryDto>, IQueryable<CountryDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetCountryQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<CountryDto>> Handle(GetQuery<CountryDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<CountryDto>(typeof(Domain.Directory.Country).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
