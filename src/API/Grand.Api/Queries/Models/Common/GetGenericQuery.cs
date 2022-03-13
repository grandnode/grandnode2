using Grand.Api.Models;
using Grand.Domain;
using MediatR;

namespace Grand.Api.Queries.Models.Common
{
    public class GetGenericQuery<T,C> : IRequest<IQueryable<T>> 
        where T : BaseApiEntityModel
        where C : BaseEntity
    {
        public string Id { get; set; }
    }
}
