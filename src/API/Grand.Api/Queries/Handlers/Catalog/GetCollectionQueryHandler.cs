using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetCollectionQueryHandler : IRequestHandler<GetQuery<CollectionDto>, IQueryable<CollectionDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetCollectionQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IQueryable<CollectionDto>> Handle(GetQuery<CollectionDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<CollectionDto>(typeof(Domain.Catalog.Collection).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
        }
    }
}
