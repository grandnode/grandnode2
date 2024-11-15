using MediatR;

namespace Grand.Module.Api.Queries.Models.Common;

public class GetQuery<T> : IRequest<IQueryable<T>> where T : class
{
    public string Id { get; set; }
}