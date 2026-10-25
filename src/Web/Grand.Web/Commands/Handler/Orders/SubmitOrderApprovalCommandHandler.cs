using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Common.Localization;
using Grand.Web.Features.Models.ShoppingCart;

namespace Grand.Web.Commands.Handler.Orders;

public class SubmitOrderApprovalCommandHandler : IRequestHandler<SubmitOrderApprovalCommand, (bool success, string error)>
{
    private readonly ICustomerService _customerService;
    private readonly IMediator _mediator;
    private readonly ICustomerMessageProviderService _messageProviderService;
    private readonly IOrderApprovalService _orderApprovalService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;
    private readonly IShoppingCartService _shoppingCartService;
    private readonly ITaxService _taxService;
    private readonly IRequestTranslationService _translationService;

    public SubmitOrderApprovalCommandHandler(
        IOrderApprovalService orderApprovalService,
        IShoppingCartService shoppingCartService,
        IProductService productService,
        IPricingService pricingService,
        ITaxService taxService,
        IPriceFormatter priceFormatter,
        IProductAttributeFormatter productAttributeFormatter,
        ICustomerService customerService,
        ICustomerMessageProviderService messageProviderService,
        IMediator mediator,
        IRequestTranslationService translationService)
    {
        _orderApprovalService = orderApprovalService;
        _shoppingCartService = shoppingCartService;
        _productService = productService;
        _pricingService = pricingService;
        _taxService = taxService;
        _priceFormatter = priceFormatter;
        _productAttributeFormatter = productAttributeFormatter;
        _customerService = customerService;
        _messageProviderService = messageProviderService;
        _mediator = mediator;
        _translationService = translationService;
    }

    public async Task<(bool success, string error)> Handle(SubmitOrderApprovalCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Store);
        ArgumentNullException.ThrowIfNull(request.Context);

        // the server works out the cart, the total and whether an approval is needed; nothing comes from the client
        var cart = await _shoppingCartService.GetShoppingCart(request.Customer, request.Store.Id,
            ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

        var state = await _orderApprovalService.GetCheckoutState(cart, request.Context);
        if (!state.Blocked || !state.Total.HasValue)
            return (false, _translationService.GetResource("OrderApproval.Submit.NotNeeded"));

        var approval = new OrderApproval {
            StoreId = request.Store.Id,
            CompanyOwnerId = request.Customer.OwnerId,
            CustomerId = request.Customer.Id,
            Status = OrderApprovalStatus.Pending,
            CartFingerprint = OrderApprovalFingerprint.Compute(cart),
            Amount = state.Total.Value,
            CurrencyCode = request.Context.Currency.CurrencyCode,
            Summary = await PrepareSummary(request, cart)
        };
        foreach (var item in await PrepareItems(request, cart))
            approval.Items.Add(item);

        // one active request per buyer and store: a new request replaces the pending or approved one
        var previous = await _orderApprovalService.GetActive(request.Customer.Id, request.Store.Id);
        if (previous != null)
        {
            previous.Status = OrderApprovalStatus.Withdrawn;
            await _orderApprovalService.Update(previous);
        }

        await _orderApprovalService.Insert(approval);

        foreach (var approver in await GetApprovers(request.Customer, request.Store.Id))
            await _messageProviderService.SendOrderApprovalRequestedMessage(approval, request.Customer, approver,
                request.Store, request.Language?.Id, request.Host);

        return (true, string.Empty);
    }

    private async Task<List<OrderApprovalItem>> PrepareItems(SubmitOrderApprovalCommand request,
        IList<ShoppingCartItem> cart)
    {
        var context = request.Context;
        var includingTax = context.TaxDisplayType == TaxDisplayType.IncludingTax;
        var items = new List<OrderApprovalItem>();

        foreach (var sci in cart)
        {
            var product = await _productService.GetProductById(sci.ProductId);
            if (product == null)
                continue;

            var item = new OrderApprovalItem {
                ProductId = product.Id,
                ProductName = product.GetTranslation(x => x.Name, request.Language?.Id),
                Sku = product.FormatSku(sci.Attributes),
                Quantity = sci.Quantity,
                // plain text: the e-mails and the views encode it when they render it
                AttributeDescription = await _productAttributeFormatter.FormatAttributes(product, sci.Attributes,
                    context, request.Language, request.Host, ", ", false, allowHyperlinks: false)
            };

            if (product.CallForPrice)
            {
                item.UnitPrice = _translationService.GetResource("Products.CallForPrice");
                item.LineTotal = item.UnitPrice;
            }
            else
            {
                var unitPrice = await _pricingService.GetUnitPrice(sci, product, context.Customer, context.Store,
                    context.Currency);
                var unitPriceShown = await _taxService.GetProductPrice(product, unitPrice.unitprice, includingTax,
                    context.Customer, context.Store);
                item.UnitPrice = _priceFormatter.FormatPrice(unitPriceShown.productprice, context.Currency);

                var subTotal = await _pricingService.GetSubTotal(sci, product, context.Customer, context.Store,
                    context.Currency);
                var subTotalShown = await _taxService.GetProductPrice(product, subTotal.subTotal, includingTax,
                    context.Customer, context.Store);
                item.LineTotal = _priceFormatter.FormatPrice(subTotalShown.productprice, context.Currency);
            }

            items.Add(item);
        }

        return items;
    }

    private async Task<OrderApprovalSummary> PrepareSummary(SubmitOrderApprovalCommand request,
        IList<ShoppingCartItem> cart)
    {
        var context = request.Context;

        // the same totals the confirm step shows
        var totals = await _mediator.Send(new GetOrderTotals {
            Customer = context.Customer,
            Language = request.Language,
            Currency = context.Currency,
            Store = context.Store,
            TaxDisplayType = context.TaxDisplayType,
            Cart = cart
        });

        // discounts on the sub total and on the order total are two lines on the confirm step; one here
        var discount = totals.SubTotalDiscountValue + totals.OrderTotalDiscountValue;

        return new OrderApprovalSummary {
            SubTotal = totals.SubTotal,
            Discount = discount != 0 ? _priceFormatter.FormatPrice(discount, context.Currency) : null,
            ShippingMethod = totals.SelectedShippingMethod,
            Shipping = totals.Shipping,
            Tax = totals.Tax,
            Total = totals.OrderTotal
        };
    }

    /// <summary>
    ///     The company owner and every active approver sub-account of the store that has an e-mail address
    /// </summary>
    private async Task<IList<Customer>> GetApprovers(Customer buyer, string storeId)
    {
        var approvers = new List<Customer>();

        var owner = await _customerService.GetCustomerById(buyer.OwnerId);
        if (owner is { Active: true, Deleted: false })
            approvers.Add(owner);

        var subAccounts = await _customerService.GetAllCustomers(ownerId: buyer.OwnerId, storeId: storeId);
        approvers.AddRange(subAccounts.Where(x => x.SubAccountRole == SubAccountRole.Approver && x.Active &&
                                                  !x.Deleted && x.StoreId == storeId));

        return approvers
            .Where(x => x.Id != buyer.Id && !string.IsNullOrWhiteSpace(x.Email))
            .DistinctBy(x => x.Id)
            .ToList();
    }
}
