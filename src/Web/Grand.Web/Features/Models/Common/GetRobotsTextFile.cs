using Grand.Mediator;

namespace Grand.Web.Features.Models.Common;

public class GetRobotsTextFile : IRequest<string>
{
    public required string StoreId { get; set; }
}