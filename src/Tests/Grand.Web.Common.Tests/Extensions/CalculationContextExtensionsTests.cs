using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.Extensions;

[TestClass]
public class CalculationContextExtensionsTests
{
    [TestMethod]
    public void CalculationContext_TakesCustomerStoreCurrencyAndTaxDisplayOfTheRequest()
    {
        //Arrange
        var customer = new Customer();
        var store = new Store { Id = "s1" };
        var currency = new Currency { CurrencyCode = "EUR" };
        var accessor = new Mock<IContextAccessor>();
        accessor.Setup(a => a.WorkContext.CurrentCustomer).Returns(customer);
        accessor.Setup(a => a.WorkContext.WorkingCurrency).Returns(currency);
        accessor.Setup(a => a.WorkContext.TaxDisplayType).Returns(TaxDisplayType.IncludingTax);
        accessor.Setup(a => a.StoreContext.CurrentStore).Returns(store);

        //Act
        var context = accessor.Object.CalculationContext();

        //Assert
        Assert.AreSame(customer, context.Customer);
        Assert.AreSame(store, context.Store);
        Assert.AreSame(currency, context.Currency);
        Assert.IsTrue(context.IncludingTax);
    }
}
