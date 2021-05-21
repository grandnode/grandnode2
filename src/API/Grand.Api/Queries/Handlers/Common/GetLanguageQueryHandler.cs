using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetLanguageQueryHandler : IRequestHandler<GetQuery<LanguageDto>, IQueryable<LanguageDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetLanguageQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }
        public async Task<IQueryable<LanguageDto>> Handle(GetQuery<LanguageDto> request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<LanguageDto>(typeof(Domain.Localization.Language).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
