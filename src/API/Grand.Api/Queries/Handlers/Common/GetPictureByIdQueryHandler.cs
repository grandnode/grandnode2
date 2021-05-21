using Grand.Api.DTOs.Common;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetPictureByIdQueryHandler : IRequestHandler<GetPictureByIdQuery, PictureDto>
    {
        private readonly IMongoDBContext _mongoDBContext;

        public GetPictureByIdQueryHandler(IMongoDBContext mongoDBContext)
        {
            _mongoDBContext = mongoDBContext;
        }

        public async Task<PictureDto> Handle(GetPictureByIdQuery request, CancellationToken cancellationToken)
        {
            var query = _mongoDBContext.Table<PictureDto>(typeof(Domain.Media.Picture).Name);
            return await Task.FromResult(query.Where(x => x.Id == request.Id).FirstOrDefault());
        }
    }
}
