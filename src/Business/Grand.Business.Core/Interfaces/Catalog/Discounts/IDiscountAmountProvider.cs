using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Plugins;

namespace Grand.Business.Core.Interfaces.Catalog.Discounts
{
    public partial interface IDiscountAmountProvider : IProvider
    {
        Task<double> DiscountAmount(Discount discount, Customer customer, Product product, double amount);
    }
}
