using Grand.Business.Core.Interfaces.Cms;
using Grand.Web.Features.Models.Common;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Common;

public class GetRobotsTextFileHandler : IRequestHandler<GetRobotsTextFile, string>
{
    private readonly IRobotsTxtService _robotsTxtService;

    public GetRobotsTextFileHandler(
        IRobotsTxtService robotsTxtService)

    {
        _robotsTxtService = robotsTxtService;
    }

    public async Task<string> Handle(GetRobotsTextFile request, CancellationToken cancellationToken)
    {
        //a store without its own robots.txt serves the one saved for "All stores" - the fallback lives
        //here, not in the service, because the admin looks a scope up exactly to insert or update it
        var robotsTxt = await _robotsTxtService.GetRobotsTxt(request.StoreId);
        if (robotsTxt == null && !string.IsNullOrEmpty(request.StoreId))
            robotsTxt = await _robotsTxtService.GetRobotsTxt("");
        return robotsTxt?.Text ?? "";
    }
}