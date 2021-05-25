using Grand.Api.DTOs.Catalog;
using Grand.Api.Queries.Models.Common;
using Grand.Domain.Data;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetSpecificationAttributeQueryHandler : IRequestHandler<GetQuery<SpecificationAttributeDto>, IQueryable<SpecificationAttributeDto>>
    {
        private readonly IDatabaseContext _dbContext;

        public GetSpecificationAttributeQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<SpecificationAttributeDto>> Handle(GetQuery<SpecificationAttributeDto> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<SpecificationAttributeDto>(typeof(Domain.Catalog.SpecificationAttribute).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));
            
        }
    }
}
