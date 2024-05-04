using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Vendors;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class VendorReviewsViewComponent : BaseViewComponent
{
    #region Constructors

    public VendorReviewsViewComponent(
        IVendorService vendorService,
        IGroupService groupService,
        IWorkContext workContext,
        IMediator mediator,
        VendorSettings vendorSettings)
    {
        _vendorService = vendorService;
        _groupService = groupService;
        _workContext = workContext;
        _mediator = mediator;
        _vendorSettings = vendorSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(string vendorId)
    {
        var vendor = await _vendorService.GetVendorById(vendorId);
        if (vendor is not { Active: true } || !vendor.AllowCustomerReviews)
            return Content("");

        var model = await _mediator.Send(new GetVendorReviews { Vendor = vendor });

        //only registered users can leave reviews
        if (await _groupService.IsGuest(_workContext.CurrentCustomer) &&
            !_vendorSettings.AllowAnonymousUsersToReviewVendor)
            model.AddVendorReview.NotAllowAnonymousUsersToReviewVendor = true;

        //default value
        model.AddVendorReview.Rating = _vendorSettings.DefaultVendorRatingValue;
        return View(model);
    }

    #endregion

    #region Fields

    private readonly IVendorService _vendorService;
    private readonly IGroupService _groupService;
    private readonly IWorkContext _workContext;
    private readonly IMediator _mediator;
    private readonly VendorSettings _vendorSettings;

    #endregion
}