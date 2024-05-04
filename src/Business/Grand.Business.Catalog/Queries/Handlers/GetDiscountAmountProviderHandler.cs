using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using MediatR;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetDiscountAmountProviderHandler : IRequestHandler<GetDiscountAmountProvider, double>
{
    private readonly IDiscountProviderLoader _discountProviderLoader;

    public GetDiscountAmountProviderHandler(IDiscountProviderLoader discountProviderLoader)
    {
        _discountProviderLoader = discountProviderLoader;
    }

    public Task<double> Handle(GetDiscountAmountProvider request, CancellationToken cancellationToken)
    {
        return GetDiscountAmountProvider(request.Discount, request.Customer, request.Product, request.Amount);
    }

    /// <summary>
    ///     Get amount from discount amount provider
    /// </summary>
    /// <param name="discount"></param>
    /// <param name="product"></param>
    /// <param name="amount"></param>
    /// <param name="customer"></param>
    /// <returns></returns>
    private async Task<double> GetDiscountAmountProvider(Discount discount, Customer customer, Product product,
        double amount)
    {
        var discountAmountProvider =
            _discountProviderLoader.LoadDiscountAmountProviderBySystemName(discount.DiscountPluginName);
        if (discountAmountProvider == null)
            return 0;
        return await discountAmountProvider.DiscountAmount(discount, customer, product, amount);
    }
}