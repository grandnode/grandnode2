using Grand.Domain.Shipping;
using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class ShippingOptionTypeConverterTests
{
    private readonly ShippingOptionTypeConverter _converter = new();

    [TestMethod]
    public void CanConvertFromTest()
    {
        Assert.IsTrue(_converter.CanConvertFrom(typeof(string)));
    }

    [TestMethod]
    public void ConvertFromTest()
    {
        var str =
            "{\"ShippingRateProviderSystemName\":\"Ground\",\"Rate\":10,\"Name\":\"sample\",\"Description\":null}";
        var result = _converter.ConvertFrom(str);
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void ConvertToTest_NotNull()
    {
        var shippingOption = new ShippingOption {
            ShippingRateProviderSystemName = "Ground",
            Name = "sample",
            Rate = 10
        };
        var result = _converter.ConvertTo(shippingOption, typeof(string));
        Assert.IsNotNull(result);
    }
}