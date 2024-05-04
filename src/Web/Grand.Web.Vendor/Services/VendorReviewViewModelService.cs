using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Vendors;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.VendorReview;
using MediatR;

namespace Grand.Web.Vendor.Services;

public class VendorReviewViewModelService : IVendorReviewViewModelService
{
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IMediator _mediator;
    private readonly ITranslationService _translationService;
    private readonly IVendorService _vendorService;
    private readonly IWorkContext _workContext;

    public VendorReviewViewModelService(
        IWorkContext workContext,
        IVendorService vendorService,
        ICustomerService customerService,
        ITranslationService translationService,
        IDateTimeService dateTimeService,
        IMediator mediator)
    {
        _vendorService = vendorService;
        _customerService = customerService;
        _translationService = translationService;
        _dateTimeService = dateTimeService;
        _mediator = mediator;
        _workContext = workContext;
    }

    public virtual async Task PrepareVendorReviewModel(VendorReviewModel model,
        VendorReview vendorReview, bool excludeProperties, bool formatReviewText)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(vendorReview);

        var customer = await _customerService.GetCustomerById(vendorReview.CustomerId);

        model.Id = vendorReview.Id;
        model.CustomerId = vendorReview.CustomerId;
        model.CustomerInfo = customer != null
            ? !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest")
            : "";
        model.Rating = vendorReview.Rating;
        model.CreatedOn = _dateTimeService.ConvertToUserTime(vendorReview.CreatedOnUtc, DateTimeKind.Utc);
        if (!excludeProperties)
        {
            model.Title = vendorReview.Title;
            model.ReviewText = formatReviewText
                ? FormatText.ConvertText(vendorReview.ReviewText)
                : vendorReview.ReviewText;
            model.IsApproved = vendorReview.IsApproved;
        }
    }

    public virtual async Task<(IEnumerable<VendorReviewModel> vendorReviewModels, int totalCount)>
        PrepareVendorReviewModel(VendorReviewListModel model, int pageIndex, int pageSize)
    {
        DateTime? createdOnFromValue = model.CreatedOnFrom == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.CreatedOnFrom.Value, _dateTimeService.CurrentTimeZone);

        DateTime? createdToFromValue = model.CreatedOnTo == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.CreatedOnTo.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var vendorReviews = await _vendorService.GetAllVendorReviews("", null,
            createdOnFromValue, createdToFromValue, model.SearchText, _workContext.CurrentVendor.Id, pageIndex - 1,
            pageSize);
        var items = new List<VendorReviewModel>();
        foreach (var x in vendorReviews)
        {
            var m = new VendorReviewModel();
            await PrepareVendorReviewModel(m, x, false, true);
            items.Add(m);
        }

        return (items, vendorReviews.TotalCount);
    }

    public virtual async Task<VendorReview> UpdateVendorReviewModel(VendorReview vendorReview, VendorReviewModel model)
    {
        vendorReview.Title = model.Title;
        vendorReview.ReviewText = model.ReviewText;
        vendorReview.IsApproved = model.IsApproved;

        await _vendorService.UpdateVendorReview(vendorReview);

        var vendor = await _vendorService.GetVendorById(vendorReview.VendorId);
        //update vendor totals
        await _vendorService.UpdateVendorReviewTotals(vendor);
        return vendorReview;
    }

    public virtual async Task DeleteVendorReview(VendorReview vendorReview)
    {
        await _vendorService.DeleteVendorReview(vendorReview);
        var vendor = await _vendorService.GetVendorById(vendorReview.VendorId);
        //update vendor totals
        await _vendorService.UpdateVendorReviewTotals(vendor);
    }

    public virtual async Task ApproveVendorReviews(IEnumerable<string> selectedIds)
    {
        foreach (var id in selectedIds)
        {
            var vendorReview = await _vendorService.GetVendorReviewById(id);
            if (vendorReview == null || vendorReview.VendorId != _workContext.CurrentVendor.Id) continue;

            var previousIsApproved = vendorReview.IsApproved;
            vendorReview.IsApproved = true;
            await _vendorService.UpdateVendorReview(vendorReview);
            await _vendorService.UpdateVendorReviewTotals(_workContext.CurrentVendor);

            //raise event (only if it wasn't approved before)
            if (!previousIsApproved)
                await _mediator.Publish(new VendorReviewApprovedEvent(vendorReview));
        }
    }

    public virtual async Task DisapproveVendorReviews(IEnumerable<string> selectedIds)
    {
        foreach (var id in selectedIds)
        {
            var vendorReview = await _vendorService.GetVendorReviewById(id);
            if (vendorReview == null || vendorReview.VendorId != _workContext.CurrentVendor.Id) continue;

            vendorReview.IsApproved = false;
            await _vendorService.UpdateVendorReview(vendorReview);
            await _vendorService.UpdateVendorReviewTotals(_workContext.CurrentVendor);
        }
    }
}