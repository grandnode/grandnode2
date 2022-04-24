using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Web.Features.Models.Common;
using MediatR;

namespace Grand.Web.Features.Handlers.Common
{
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
            var robotsTxt = await _robotsTxtService.GetRobotsTxt(request.StoreId);
            if (robotsTxt != null)
                return robotsTxt.Text;

            return "";
        }
    }
}
