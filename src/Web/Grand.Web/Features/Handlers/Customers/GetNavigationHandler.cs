using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Customers
{
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
            var model = new CustomerNavigationModel();
            model.HideLoyaltyPoints = !_loyaltyPointsSettings.Enabled;
            model.HideDeleteAccount = !_customerSettings.AllowUsersToDeleteAccount;
            model.HideMerchandiseReturns = !_orderSettings.MerchandiseReturnsEnabled;
            model.HideDownloadableProducts = _customerSettings.HideDownloadableProductsTab;
            model.HideOutOfStockSubscriptions = _customerSettings.HideOutOfStockSubscriptionsTab;
            model.HideAuctions = _customerSettings.HideAuctionsTab;
            model.HideNotes = _customerSettings.HideNotesTab;
            model.HideDocuments = _customerSettings.HideDocumentsTab;
            model.HideReviews = _customerSettings.HideReviewsTab;
            model.HideCourses = _customerSettings.HideCoursesTab;
            model.HideSubAccounts = _customerSettings.HideSubAccountsTab || !string.IsNullOrEmpty(request.Customer.OwnerId);
            if (_vendorSettings.AllowVendorsToEditInfo && request.Vendor != null)
            {
                model.ShowVendorInfo = true;
            }
            model.SelectedTab = (AccountNavigationEnum)request.SelectedTabId;

            return await Task.FromResult(model);
        }
    }
}
