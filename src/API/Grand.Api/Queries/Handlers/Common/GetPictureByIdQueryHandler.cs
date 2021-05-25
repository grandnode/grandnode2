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
        private readonly IDatabaseContext _dbContext;

        public GetPictureByIdQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PictureDto> Handle(GetPictureByIdQuery request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<PictureDto>(typeof(Domain.Media.Picture).Name);
            return await Task.FromResult(query.Where(x => x.Id == request.Id).FirstOrDefault());
        }
    }
}
