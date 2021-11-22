using MediatR;

namespace Grand.Web.Features.Models.Common
{
    public class GetRobotsTextFile : IRequest<string>
    {
        public string StoreId { get; set; }
    }
}
