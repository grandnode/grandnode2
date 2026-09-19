using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Grand.Infrastructure.Security;
using Grand.Web.Common.Components;
using Grand.Web.Store.Models.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Store.Components;

public class StorePageViewComponent : BaseStoreViewComponent
{
    #region Fields

    private readonly IPageService _pageService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IHtmlSanitizationService _htmlSanitizationService;
    private readonly ILogger<StorePageViewComponent> _logger;

    #endregion

    #region Constructors

    public StorePageViewComponent(
        IPageService pageService,
        IContextAccessor contextAccessor,
        IHtmlSanitizationService htmlSanitizationService,
        ILogger<StorePageViewComponent> logger)
    {
        _pageService = pageService;
        _contextAccessor = contextAccessor;
        _htmlSanitizationService = htmlSanitizationService;
        _logger = logger;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string systemName)
    {
        var page = await _pageService.GetPageBySystemName(systemName,
            _contextAccessor.StoreContext.CurrentStore.Id);
        var body = page?.Body;
        //the body is rendered unencoded on the dashboard; it is checked against the rich-text allowlist
        //when it is saved, and here too for data written around the form (import, API, database) -
        //the same rule as AdminPageViewComponent
        if (!string.IsNullOrEmpty(body) && _htmlSanitizationService.ContainsDisallowedRichText(body))
        {
            _logger.LogWarning("The body of page {SystemName} contains markup outside the allowlist and was not rendered",
                systemName);
            body = null;
        }

        var model = new StorePortalModel(page?.Title, body);
        return View(model);
    }

    #endregion
}
