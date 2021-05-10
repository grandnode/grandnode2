using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetLayoutQueryHandler : IRequestHandler<GetLayoutQuery, IMongoQueryable<LayoutDto>>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetLayoutQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }

        public Task<IMongoQueryable<LayoutDto>> Handle(GetLayoutQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.Id))
                return Task.FromResult(_mongoDBContext.Database().GetCollection<LayoutDto>(request.LayoutName).AsQueryable());
            else
                return Task.FromResult(_mongoDBContext.Database().GetCollection<LayoutDto>(request.LayoutName).AsQueryable().Where(x => x.Id == request.Id));
        }
    }
}
