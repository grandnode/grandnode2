using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Store.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Components;

public class StorePageViewComponent : BaseStoreViewComponent
{
    #region Fields

    private readonly IPageService _pageService;
    private readonly IContextAccessor _contextAccessor;

    #endregion

    #region Constructors

    public StorePageViewComponent(
        IPageService pageService,
        IContextAccessor contextAccessor)
    {
        _pageService = pageService;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string systemName)
    {
        var page = await _pageService.GetPageBySystemName(systemName,
            _contextAccessor.StoreContext.CurrentStore.Id);
        var model = new StorePortalModel(page?.Title, page?.Body);
        return View(model);
    }

    #endregion
}