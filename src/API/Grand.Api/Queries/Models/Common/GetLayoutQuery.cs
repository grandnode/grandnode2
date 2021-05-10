using Grand.Api.DTOs.Common;
using MediatR;
using MongoDB.Driver.Linq;

namespace Grand.Api.Queries.Models.Common
{
    public class GetLayoutQuery : IRequest<IMongoQueryable<LayoutDto>>
    {
        public string Id { get; set; }
        public string LayoutName { get; set; }
    }
}
