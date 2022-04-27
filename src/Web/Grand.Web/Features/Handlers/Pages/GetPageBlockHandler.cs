using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Infrastructure;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Pages;
using Grand.Web.Models.Pages;
using MediatR;

namespace Grand.Web.Features.Handlers.Pages
{
    public class GetPageBlockHandler : IRequestHandler<GetPageBlock, PageModel>
    {
        private readonly IPageService _pageService;
        private readonly IWorkContext _workContext;
        private readonly IAclService _aclService;
        private readonly IDateTimeService _dateTimeService;

        public GetPageBlockHandler(
            IPageService pageService,
            IWorkContext workContext,
            IAclService aclService,
            IDateTimeService dateTimeService)
        {
            _pageService = pageService;
            _workContext = workContext;
            _aclService = aclService;
            _dateTimeService = dateTimeService;
        }

        public async Task<PageModel> Handle(GetPageBlock request, CancellationToken cancellationToken)
        {
            //load by store
            var page = string.IsNullOrEmpty(request.PageId) ?
                await _pageService.GetPageBySystemName(request.SystemName, _workContext.CurrentStore.Id) :
                await _pageService.GetPageById(request.PageId);

            if (page == null || !page.Published)
                return null;

            if ((page.StartDateUtc.HasValue && page.StartDateUtc > DateTime.UtcNow) || (page.EndDateUtc.HasValue && page.EndDateUtc < DateTime.UtcNow))
                return null;

            //ACL (access control list)
            if (!_aclService.Authorize(page, _workContext.CurrentCustomer))
                return null;

            return page.ToModel(_workContext.WorkingLanguage, _dateTimeService, request.Password);

        }
    }
}
