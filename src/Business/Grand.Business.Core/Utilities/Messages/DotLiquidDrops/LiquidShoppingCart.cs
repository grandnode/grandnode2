using DotLiquid;
using Grand.Domain.Common;
using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Messages.DotLiquidDrops;

public class LiquidShoppingCart : Drop
{
    private readonly Customer _customer;

    public LiquidShoppingCart(Customer customer)
    {
        _customer = customer;
        Items = new List<LiquidShoppingCartItem>();
        AdditionalTokens = new Dictionary<string, string>();
    }

    public string Email => _customer.Email;

    public string Username => _customer.Username;

    public string FullName => _customer.GetFullName();

    public string FirstName => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.FirstName);

    public string LastName => _customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LastName);

    public ICollection<LiquidShoppingCartItem> Items { get; }

    public IDictionary<string, string> AdditionalTokens { get; set; }
}