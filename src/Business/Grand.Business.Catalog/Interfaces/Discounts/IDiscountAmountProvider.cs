using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Infrastructure.Plugins;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Interfaces.Discounts
{
    public partial interface IDiscountAmountProvider : IProvider
    {
        Task<double> DiscountAmount(Discount discount, Customer customer, Product product, double amount);
    }
}
