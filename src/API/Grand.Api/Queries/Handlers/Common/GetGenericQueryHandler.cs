using Grand.Api.Models;
using Grand.Api.Queries.Models.Common;
using Grand.Domain;
using Grand.Domain.Data;
using MediatR;

namespace Grand.Api.Queries.Handlers.Common
{
    public class GetGenericQueryHandler<T,C> : IRequestHandler<GetGenericQuery<T,C>, IQueryable<T>> 
        where T : BaseApiEntityModel
        where C : BaseEntity
    {
        private readonly IDatabaseContext _dbContext;

        public GetGenericQueryHandler(IDatabaseContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IQueryable<T>> Handle(GetGenericQuery<T,C> request, CancellationToken cancellationToken)
        {
            var query = _dbContext.Table<T>(typeof(C).Name);

            if (string.IsNullOrEmpty(request.Id))
                return query;
            else
                return await Task.FromResult(query.Where(x => x.Id == request.Id));

        }
    }
}
