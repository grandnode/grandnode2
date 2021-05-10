using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetCollectionQueryHandler : IRequestHandler<GetQuery<CollectionDto>, IMongoQueryable<CollectionDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetCollectionQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }

        public Task<IMongoQueryable<CollectionDto>> Handle(GetQuery<CollectionDto> request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
                return Task.FromResult(
                    _mongoDBContext.Database()
                    .GetCollection<CollectionDto>
                    (typeof(Domain.Catalog.Collection).Name)
                    .AsQueryable());
            else
                return Task.FromResult(_mongoDBContext.Database()
                    .GetCollection<CollectionDto>(typeof(Domain.Catalog.Collection).Name)
                    .AsQueryable()
                    .Where(x => x.Id == request.Id));
        }
    }
}
