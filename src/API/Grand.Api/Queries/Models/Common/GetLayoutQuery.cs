using Grand.Api.DTOs.Common;
using MediatR;
using System.Linq;

namespace Grand.Api.Queries.Models.Common
{
    public class GetLayoutQuery : IRequest<IQueryable<LayoutDto>>
    {
        public string Id { get; set; }
        public string LayoutName { get; set; }
    }
}
