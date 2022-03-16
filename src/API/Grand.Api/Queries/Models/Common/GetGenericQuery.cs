using Grand.Api.Models;
using Grand.Domain;
using MediatR;

namespace Grand.Api.Queries.Models.Common;

public record GetGenericQuery<T, C>(string Id = null) : IRequest<IQueryable<T>> where T : BaseApiEntityModel where C : BaseEntity;


