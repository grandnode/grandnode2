using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Configuration;
using Grand.Web.Features.Models.Customers;
using Grand.Web.Models.Customer;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Customers;

public class GetNavigationHandler : IRequestHandler<GetNavigation, CustomerNavigationModel>
{
    private readonly CustomerSettings _customerSettings;
    private readonly ICustomerService _customerService;
    private readonly LoyaltyPointsSettings _loyaltyPointsSettings;
    private readonly OidcLoginConfig _loginConfig;
    private readonly OrderSettings _orderSettings;
    private readonly IRecurringPaymentService _recurringPaymentService;
    private readonly VendorSettings _vendorSettings;

    public GetNavigationHandler(
        CustomerSettings customerSettings,
        LoyaltyPointsSettings loyaltyPointsSettings,
        OrderSettings orderSettings,
        VendorSettings vendorSettings,
        OidcLoginConfig loginConfig,
        ICustomerService customerService,
        IRecurringPaymentService recurringPaymentService)
    {
        _customerSettings = customerSettings;
        _loyaltyPointsSettings = loyaltyPointsSettings;
        _orderSettings = orderSettings;
        _vendorSettings = vendorSettings;
        _loginConfig = loginConfig;
        _customerService = customerService;
        _recurringPaymentService = recurringPaymentService;
    }

    public async Task<CustomerNavigationModel> Handle(GetNavigation request, CancellationToken cancellationToken)
    {
        var model = new CustomerNavigationModel {
            HideChangePassword = _loginConfig.ProviderOnly,
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
            HideOrderApprovals = _customerSettings.HideSubAccountsTab || !await IsInCompany(request.Customer),
            HideRecurringPayments = _customerSettings.HideRecurringPaymentsTab ||
                                    !await HasRecurringPayments(request.Customer, request.Store),
            SelectedTab = (AccountNavigationEnum)request.SelectedTabId
        };

        return model;
    }

    /// <summary>
    ///     A sub-account always belongs to a company; an owner does once it has a sub-account
    /// </summary>
    private async Task<bool> IsInCompany(Customer customer)
    {
        if (!string.IsNullOrEmpty(customer.OwnerId))
            return true;

        var subAccounts = await _customerService.GetAllCustomers(ownerId: customer.Id, pageSize: 1);
        return subAccounts.TotalCount > 0;
    }

    /// <summary>
    ///     Recurring products cannot be bought until a payment provider charges in cycles, so a tab every customer
    ///     would find empty is shown only to those who have a recurring payment
    /// </summary>
    private async Task<bool> HasRecurringPayments(Customer customer, Domain.Stores.Store store)
    {
        if (store == null)
            return false;

        var recurringPayments = await _recurringPaymentService.GetCustomerRecurringPayments(customer.Id, store.Id, 0, 1);
        return recurringPayments.TotalCount > 0;
    }
}