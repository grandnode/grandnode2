using Grand.Business.Core.Events.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Mediator;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Events.Orders;

/// <summary>
///     Records the approval request of an order placed awaiting approval and asks the company's approvers to decide it
/// </summary>
public class OrderPlacedOrderApprovalHandler : INotificationHandler<OrderPlacedEvent>
{
    private const string AdminPanelArea = "Admin";
    private const string StorePanelArea = "Store";

    private readonly ICurrencyService _currencyService;
    private readonly ICustomerService _customerService;
    private readonly CustomerSettings _customerSettings;
    private readonly LanguageSettings _languageSettings;
    private readonly ISalesEmployeeService _salesEmployeeService;
    private readonly ILogger<OrderPlacedOrderApprovalHandler> _logger;
    private readonly ICustomerMessageProviderService _messageProviderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;
    private readonly IStoreService _storeService;

    public OrderPlacedOrderApprovalHandler(
        IOrderApprovalService orderApprovalService,
        ICustomerService customerService,
        ICustomerMessageProviderService messageProviderService,
        IStoreService storeService,
        IProductService productService,
        IPriceFormatter priceFormatter,
        ICurrencyService currencyService,
        ISalesEmployeeService salesEmployeeService,
        CustomerSettings customerSettings,
        LanguageSettings languageSettings,
        ILogger<OrderPlacedOrderApprovalHandler> logger)
    {
        _salesEmployeeService = salesEmployeeService;
        _customerSettings = customerSettings;
        _languageSettings = languageSettings;
        _orderApprovalService = orderApprovalService;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _storeService = storeService;
        _productService = productService;
        _priceFormatter = priceFormatter;
        _currencyService = currencyService;
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        if (order.OrderStatusId != (int)OrderStatusSystem.AwaitingApproval)
            return;

        try
        {
            await CreateApprovalRequest(order, notification.ApprovalReasons);
        }
        catch (Exception ex)
        {
            // the order is already placed: publishing must complete whatever happens here
            _logger.LogError(ex,
                "Order {OrderNumber} ({OrderId}) awaits approval but its approval request could not be created",
                order.OrderNumber, order.Id);
        }
    }

    private async Task CreateApprovalRequest(Order order, IReadOnlyList<string> reasons)
    {
        // one request per order, even if the event is raised twice
        if (await _orderApprovalService.GetByOrderId(order.Id) != null)
            return;

        var buyer = await _customerService.GetCustomerById(order.CustomerId);
        if (buyer == null)
        {
            _logger.LogError(
                "Order {OrderNumber} ({OrderId}) awaits approval but its approval request could not be created: buyer {CustomerId} not found",
                order.OrderNumber, order.Id, order.CustomerId);
            return;
        }

        // a sub-account's company decides, while the storefront has the approvals screen; anyone else has no company
        // to ask. Not the order's OwnerId: it holds the customer's own id when there is no owner
        var companyDecides = !_customerSettings.HideSubAccountsTab && !string.IsNullOrEmpty(buyer.OwnerId);
        var currency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);

        var approval = new OrderApproval {
            StoreId = order.StoreId,
            Decider = companyDecides ? OrderApprovalDecider.Company : OrderApprovalDecider.Store,
            CompanyOwnerId = companyDecides ? buyer.OwnerId : null,
            CustomerId = order.CustomerId,
            Status = OrderApprovalStatus.Pending,
            OrderId = order.Id,
            Amount = order.OrderTotal,
            CurrencyCode = order.CustomerCurrencyCode,
            Reasons = reasons.ToList(),
            Summary = PrepareSummary(order, currency)
        };
        foreach (var item in await PrepareItems(order, currency))
            approval.Items.Add(item);

        await _orderApprovalService.Insert(approval);

        var store = await _storeService.GetStoreById(order.StoreId);
        if (!companyDecides)
        {
            await NotifyStore(approval, order, buyer, store);
            return;
        }

        foreach (var approver in await GetApprovers(buyer, buyer.OwnerId, order.StoreId))
            try
            {
                await _messageProviderService.SendOrderApprovalRequestedMessage(approval, buyer, approver, store,
                    order.CustomerLanguageId);
            }
            catch (Exception ex)
            {
                // a failed e-mail must not stop the others, nor the order
                _logger.LogError(ex, "Order approval request e-mail failed. Order {OrderId}, approver {ApproverId}",
                    order.Id, approver.Id);
            }
    }

    /// <summary>
    ///     The e-mail only tells someone in the store: anyone with access to the order may decide it
    /// </summary>
    private async Task NotifyStore(OrderApproval approval, Order order, Customer buyer, Store store)
    {
        try
        {
            var (email, name, panelArea) = await GetStoreRecipient(order, store);
            await _messageProviderService.SendOrderApprovalStaffMessage(approval, buyer, store, email, name, panelArea,
                _languageSettings.DefaultAdminLanguageId);
        }
        catch (Exception ex)
        {
            // a failed e-mail must not stop the order
            _logger.LogError(ex, "Order approval e-mail to the store failed. Order {OrderId}", order.Id);
        }
    }

    /// <summary>
    ///     The order's sales employee, else the store's owner account, else nobody named: the message then goes to
    ///     the e-mail account of its template
    /// </summary>
    private async Task<(string Email, string Name, string PanelArea)> GetStoreRecipient(Order order, Store store)
    {
        if (!string.IsNullOrEmpty(order.SeId))
        {
            var salesEmployee = await _salesEmployeeService.GetSalesEmployeeById(order.SeId);
            if (salesEmployee is { Active: true } && !string.IsNullOrWhiteSpace(salesEmployee.Email))
                return (salesEmployee.Email, salesEmployee.Name, AdminPanelArea);
        }

        if (!string.IsNullOrEmpty(store.OwnerCustomerId))
        {
            var owner = await _customerService.GetCustomerById(store.OwnerCustomerId);
            if (owner is { Active: true, Deleted: false } && !string.IsNullOrWhiteSpace(owner.Email))
                return (owner.Email, owner.GetFullName(),
                    string.IsNullOrEmpty(owner.StaffStoreId) ? AdminPanelArea : StorePanelArea);
        }

        return (null, null, AdminPanelArea);
    }

    private async Task<List<OrderApprovalItem>> PrepareItems(Order order, Currency currency)
    {
        var includingTax = order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
        var items = new List<OrderApprovalItem>();

        foreach (var orderItem in order.OrderItems)
        {
            var product = await _productService.GetProductById(orderItem.ProductId);
            items.Add(new OrderApprovalItem {
                ProductId = orderItem.ProductId,
                ProductName = product?.GetTranslation(x => x.Name, order.CustomerLanguageId),
                Sku = orderItem.Sku,
                Quantity = orderItem.Quantity,
                AttributeDescription = ToPlainText(orderItem.AttributeDescription),
                UnitPrice = _priceFormatter.FormatPrice(includingTax ? orderItem.UnitPriceInclTax : orderItem.UnitPriceExclTax,
                    currency),
                LineTotal = _priceFormatter.FormatPrice(includingTax ? orderItem.PriceInclTax : orderItem.PriceExclTax,
                    currency)
            });
        }

        return items;
    }

    private OrderApprovalSummary PrepareSummary(Order order, Currency currency)
    {
        var includingTax = order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;
        var discount = includingTax
            ? order.OrderSubTotalDiscountInclTax + order.OrderDiscount
            : order.OrderSubTotalDiscountExclTax + order.OrderDiscount;

        return new OrderApprovalSummary {
            SubTotal = _priceFormatter.FormatPrice(includingTax ? order.OrderSubtotalInclTax : order.OrderSubtotalExclTax,
                currency),
            Discount = discount != 0 ? _priceFormatter.FormatPrice(discount, currency) : null,
            ShippingMethod = order.ShippingMethod,
            Shipping = _priceFormatter.FormatPrice(includingTax ? order.OrderShippingInclTax : order.OrderShippingExclTax,
                currency),
            Tax = _priceFormatter.FormatPrice(order.OrderTax, currency),
            Total = _priceFormatter.FormatPrice(order.OrderTotal, currency)
        };
    }

    /// <summary>
    ///     The attribute description carries markup; the approval stores plain text, the views and e-mails encode it
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex LineBreak = new("<br\\s*/?>",
        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

    private static readonly System.Text.RegularExpressions.Regex Tag = new("<[^>]*>",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private static string ToPlainText(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return html;

        var lines = LineBreak.Split(html)
            .Select(x => Tag.Replace(x, string.Empty))
            .Select(x => System.Net.WebUtility.HtmlDecode(x).Trim())
            .Where(x => x.Length > 0);
        return string.Join(", ", lines);
    }

    /// <summary>
    ///     The company owner and every active approver sub-account of the store that has an e-mail address;
    ///     never the buyer
    /// </summary>
    private async Task<IList<Customer>> GetApprovers(Customer buyer, string companyOwnerId, string storeId)
    {
        var approvers = new List<Customer>();

        var owner = await _customerService.GetCustomerById(companyOwnerId);
        if (owner is { Active: true, Deleted: false })
            approvers.Add(owner);

        var subAccounts = await _customerService.GetAllCustomers(ownerId: companyOwnerId, storeId: storeId);
        approvers.AddRange(subAccounts.Where(x => x.SubAccountRole == SubAccountRole.Approver && x is { Active: true, Deleted: false }));

        return approvers
            .Where(x => x.Id != buyer.Id && !string.IsNullOrWhiteSpace(x.Email))
            .DistinctBy(x => x.Id)
            .ToList();
    }
}
