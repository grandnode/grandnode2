using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Catalog;

[TestClass]
public class TierPriceExtensionsTests
{
    private readonly IList<TierPrice> _tierPrices;

    public TierPriceExtensionsTests()
    {
        _tierPrices = new List<TierPrice>();
        _tierPrices.Add(new TierPrice {
            CurrencyCode = "USD",
            CustomerGroupId = null,
            Price = 10,
            Quantity = 1,
            StoreId = "1"
        });
        _tierPrices.Add(new TierPrice {
            CurrencyCode = "USD",
            CustomerGroupId = "1",
            Price = 12,
            Quantity = 1,
            StoreId = "1"
        });
        _tierPrices.Add(new TierPrice {
            CurrencyCode = "USD",
            CustomerGroupId = "2",
            Price = 10,
            Quantity = 1,
            StoreId = "2",
            EndDateTimeUtc = DateTime.UtcNow.AddDays(1)
        });
        _tierPrices.Add(new TierPrice {
            CurrencyCode = "EUR",
            CustomerGroupId = "3",
            Price = 10,
            Quantity = 1,
            StoreId = "3",
            StartDateTimeUtc = DateTime.UtcNow
        });
    }

    [TestMethod]
    public void FilterByDateTest()
    {
        Assert.AreEqual(3, _tierPrices.FilterByDate(new DateTime(2010, 01, 01)).Count());
    }

    [TestMethod]
    public void FilterByStoreTest()
    {
        Assert.AreEqual(2, _tierPrices.FilterByStore("1").Count());
    }

    [TestMethod]
    public void FilterForCustomerTest()
    {
        var customer = new Customer();
        customer.Groups.Add("1");
        customer.Groups.Add("2");
        Assert.AreEqual(3, _tierPrices.FilterForCustomer(customer).Count());
    }

    [TestMethod]
    public void FilterByCurrencyTest()
    {
        Assert.AreEqual(3, _tierPrices.FilterByCurrency("USD").Count());
    }

    [TestMethod]
    public void RemoveDuplicatedQuantitiesTest()
    {
        var tierPrices = new List<TierPrice>();
        tierPrices.Add(new TierPrice {
            //will be removed
            Id = "1",
            Price = 150,
            Quantity = 1
        });
        tierPrices.Add(new TierPrice {
            //will stay
            Id = "2",
            Price = 100,
            Quantity = 1
        });
        tierPrices.Add(new TierPrice {
            //will stay
            Id = "3",
            Price = 200,
            Quantity = 3
        });
        tierPrices.Add(new TierPrice {
            //will stay
            Id = "4",
            Price = 250,
            Quantity = 4
        });
        tierPrices.Add(new TierPrice {
            //will be removed
            Id = "5",
            Price = 300,
            Quantity = 4
        });
        tierPrices.Add(new TierPrice {
            //will stay
            Id = "6",
            Price = 350,
            Quantity = 5
        });

        var tierPriceCollection = tierPrices.RemoveDuplicatedQuantities();

        Assert.IsNull(tierPriceCollection.FirstOrDefault(v => v.Id == "1"));
        Assert.IsNotNull(tierPriceCollection.FirstOrDefault(v => v.Id == "2")); //doubled 15 - saved
        Assert.IsNotNull(tierPriceCollection.FirstOrDefault(v => v.Id == "3")); //! doubled 15 - removed
        Assert.IsNotNull(tierPriceCollection.FirstOrDefault(v => v.Id == "4")); //doubled 23 - saved
        Assert.IsNull(tierPriceCollection.FirstOrDefault(v => v.Id == "5"));
        Assert.IsNotNull(tierPriceCollection.FirstOrDefault(v => v.Id == "6")); //! doubled 23 - removed        }
    }
}