using Grand.Domain.Shipping;
using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter
{
    [TestClass()]
    public class ShippingOptionTypeConverterTests
    {
        ShippingOptionTypeConverter _converter;

        public ShippingOptionTypeConverterTests()
        {
            _converter = new ShippingOptionTypeConverter();
        }

        [TestMethod()]
        public void CanConvertFromTest()
        {
            Assert.IsTrue(_converter.CanConvertFrom(typeof(string)));
        }

        [TestMethod()]
        public void ConvertFromTest()
        {
            var str = "{\"ShippingRateProviderSystemName\":\"Ground\",\"Rate\":10,\"Name\":\"sample\",\"Description\":null}";
            var result = _converter.ConvertFrom(str);
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public void ConvertToTest_NotNull()
        {
            ShippingOption shippingOption = new ShippingOption();
            shippingOption.ShippingRateProviderSystemName = "Ground";
            shippingOption.Name = "sample";
            shippingOption.Rate = 10;
            var result = _converter.ConvertTo(shippingOption, typeof(string));
            Assert.IsNotNull(result);
        }
    }
}