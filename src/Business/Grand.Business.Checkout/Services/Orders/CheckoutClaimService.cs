using Grand.Business.Checkout.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Data;
using Grand.Domain.Discounts;
using Microsoft.Extensions.Logging;

namespace Grand.Business.Checkout.Services.Orders;

/// <summary>
///     Checkout claim service
/// </summary>
public class CheckoutClaimService : ICheckoutClaimService
{
    private readonly IDiscountService _discountService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly IInventoryManageService _inventoryManageService;
    private readonly ILogger<CheckoutClaimService> _logger;
    private readonly ILoyaltyPointsService _loyaltyPointsService;
    private readonly IProductService _productService;

    public CheckoutClaimService(IInventoryManageService inventoryManageService,
        IDiscountService discountService,
        IGiftVoucherService giftVoucherService,
        ILoyaltyPointsService loyaltyPointsService,
        IProductService productService,
        ILogger<CheckoutClaimService> logger)
    {
        _inventoryManageService = inventoryManageService;
        _discountService = discountService;
        _giftVoucherService = giftVoucherService;
        _loyaltyPointsService = loyaltyPointsService;
        _productService = productService;
        _logger = logger;
    }

    public virtual async Task<CheckoutClaim> Claim(PlaceOrderContainer details, string orderCode)
    {
        ArgumentNullException.ThrowIfNull(details);

        var claim = new CheckoutClaim {
            OrderId = details.OrderId,
            OrderCode = orderCode,
            CustomerId = details.Customer.Id,
            StoreId = details.Store.Id
        };
        try
        {
            claim.Succeeded = await ClaimStock(details, claim)
                              && await ClaimDiscounts(details, claim)
                              && await ClaimGiftVouchers(details, claim)
                              && await ClaimLoyaltyPoints(details, claim);
        }
        catch (ResourceLockTimeoutException ex)
        {
            //another checkout held a resource for the whole wait: a refusal, so the buyer is asked to try again; a
            //database timeout is a failure and goes on as one
            _logger.LogWarning(ex, "A resource of order {OrderId} stayed in use by another checkout", claim.OrderId);
            claim.Succeeded = false;
        }
        catch
        {
            await Release(claim);
            throw;
        }

        if (!claim.Succeeded)
            await Release(claim);

        return claim;
    }

    public virtual async Task Release(CheckoutClaim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        //in the opposite order of the claim
        if (claim.LoyaltyPoints > 0)
            await GiveBack(claim, "loyalty points", () => _loyaltyPointsService.AddLoyaltyPointsHistory(
                claim.CustomerId, claim.LoyaltyPoints, claim.StoreId,
                $"Loyalty points returned for order {claim.OrderCode}"));
        claim.LoyaltyPoints = 0;

        foreach (var giftVoucherId in claim.GiftVoucherIds.Reverse())
            await GiveBack(claim, "gift voucher",
                () => _giftVoucherService.CancelRedemption(giftVoucherId, claim.OrderId));
        claim.GiftVoucherIds.Clear();

        foreach (var usage in claim.DiscountUsages.Reverse())
            await GiveBack(claim, "discount", () => _discountService.ReleaseDiscountUsageHistory(usage));
        claim.DiscountUsages.Clear();

        foreach (var stock in claim.Stock.Reverse())
            await GiveBack(claim, "stock", () => _inventoryManageService.CancelReservation(stock.Product,
                stock.Quantity, stock.Attributes, stock.WarehouseId));
        claim.Stock.Clear();
    }

    private async Task<bool> ClaimStock(PlaceOrderContainer details, CheckoutClaim claim)
    {
        foreach (var sc in details.Cart)
        {
            var product = await _productService.GetProductById(sc.ProductId);
            var warehouseId = OrderItemWarehouse.Resolve(sc, product, details.Store.DefaultWarehouseId);
            if (!await _inventoryManageService.TryReserve(product, sc.Quantity, sc.Attributes, warehouseId))
                return false;

            claim.Stock.Add(new ClaimedStock(product, sc.Quantity, sc.Attributes, warehouseId));
        }

        return true;
    }

    private async Task<bool> ClaimDiscounts(PlaceOrderContainer details, CheckoutClaim claim)
    {
        foreach (var appliedDiscount in details.AppliedDiscounts)
        {
            //a discount deleted meanwhile has no limit left to keep
            var discount = await _discountService.GetDiscountById(appliedDiscount.DiscountId);
            if (discount == null)
                continue;

            var usage = new DiscountUsageHistory {
                DiscountId = appliedDiscount.DiscountId,
                CouponCode = appliedDiscount.CouponCode,
                OrderId = details.OrderId,
                CustomerId = details.Customer.Id
            };
            if (!await _discountService.TryInsertDiscountUsageHistory(discount, usage))
                return false;

            claim.DiscountUsages.Add(usage);
        }

        return true;
    }

    private async Task<bool> ClaimGiftVouchers(PlaceOrderContainer details, CheckoutClaim claim)
    {
        foreach (var appliedGiftVoucher in details.AppliedGiftVouchers ?? [])
        {
            if (!await _giftVoucherService.TryRedeem(appliedGiftVoucher.GiftVoucher.Id,
                    appliedGiftVoucher.AmountCanBeUsed, details.OrderId))
                return false;

            claim.GiftVoucherIds.Add(appliedGiftVoucher.GiftVoucher.Id);
        }

        return true;
    }

    private async Task<bool> ClaimLoyaltyPoints(PlaceOrderContainer details, CheckoutClaim claim)
    {
        if (details.RedeemedLoyaltyPointsAmount <= 0)
            return true;

        var redeemed = await _loyaltyPointsService.TryRedeemLoyaltyPoints(details.Customer.Id,
            details.RedeemedLoyaltyPoints, details.Store.Id, $"Loyalty points redeemed for order {claim.OrderCode}",
            details.OrderId, details.RedeemedLoyaltyPointsAmount);
        if (redeemed == null)
            return false;

        claim.LoyaltyPoints = details.RedeemedLoyaltyPoints;
        return true;
    }

    private async Task GiveBack(CheckoutClaim claim, string resource, Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not give back the {Resource} claimed for order {OrderId}", resource,
                claim.OrderId);
        }
    }
}
