using Grand.Business.Core.Interfaces.Cms;
using Grand.Web.Common.Components;
using Grand.Web.Vendor.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Components;

public class VendorPageViewComponent : BaseVendorViewComponent
{
    #region Fields

    private readonly IPageService _pageService;

    #endregion

    #region Constructors

    public VendorPageViewComponent(
        IPageService pageService)
    {
        _pageService = pageService;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string systemName)
    {
        var page = await _pageService.GetPageBySystemName(systemName);
        var model = new VendorPortalModel(page?.Title, page?.Body);
        return View(model);
    }

    #endregion
}