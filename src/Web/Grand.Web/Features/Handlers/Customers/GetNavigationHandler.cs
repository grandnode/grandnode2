using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;

namespace Grand.Web.Features.Handlers.Customers;

public class GetNavigationHandler : IRequestHandler<GetNavigation, CustomerNavigationModel>
{
    private readonly CustomerSettings _customerSettings;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly OrderSettings _orderSettings;
    private readonly VendorSettings _vendorSettings;

    public GetNavigationHandler(
        CustomerSettings customerSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        OrderSettings orderSettings,
        VendorSettings vendorSettings)
    {
        _customerSettings = customerSettings;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _orderSettings = orderSettings;
        _vendorSettings = vendorSettings;
    }

    public async Task<CustomerNavigationModel> Handle(GetNavigation request, CancellationToken cancellationToken)
    {
        var model = new CustomerNavigationModel {
            HideLoyaltyPoints = !_loyaltyPointsSettings.Enabled,
            HideDeleteAccount = !_customerSettings.AllowUsersToDeleteAccount,
            HideMerchandiseReturns = !_orderSettings.MerchandiseReturnsEnabled,
            HideDownloadableProducts = _customerSettings.HideDownloadableProductsTab,
            HideOutOfStockSubscriptions = _customerSettings.HideOutOfStockSubscriptionsTab,
            HideAuctions = _customerSettings.HideAuctionsTab,
            HideNotes = _customerSettings.HideNotesTab,
            HideDocuments = _customerSettings.HideDocumentsTab,
            HideReviews = _customerSettings.HideReviewsTab,
            HideCourses = _customerSettings.HideCoursesTab,
            HideSubAccounts = _customerSettings.HideSubAccountsTab || !string.IsNullOrEmpty(request.Customer.OwnerId),
            SelectedTab = (AccountNavigationEnum)request.SelectedTabId
        };

        return await Task.FromResult(model);
    }
}