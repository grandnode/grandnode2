using Grand.Domain.Common;
using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter
{
    [TestClass()]
    public class CustomAttributeListTypeConverterTests
    {
        CustomAttributeListTypeConverter _converter;

        public CustomAttributeListTypeConverterTests()
        {
            _converter = new CustomAttributeListTypeConverter();
        }

        [TestMethod()]
        public void CanConvertFromTest_True()
        {
            Assert.IsTrue(_converter.CanConvertFrom(typeof(string)));
        }

        [TestMethod()]
        public void CanConvertFromTest_False()
        {
            Assert.IsFalse(_converter.CanConvertFrom(typeof(decimal)));
        }

        [TestMethod()]
        public void ConvertFromTest_NotNull()
        {
            var str = "[{\"Key\":\"FirstName\",\"Value\":\"Lucas\"},{\"Key\":\"LastName\",\"Value\":\"Scott\"}]";
            var converted = _converter.ConvertFrom(str);
            Assert.IsNotNull(converted);
        }
        [TestMethod()]
        public void ConvertFromTest_Null()
        {
            var converted = _converter.ConvertFrom("test");
            Assert.IsNull(converted);
        }

        [TestMethod()]
        public void ConvertToTest()
        {
            List<CustomAttribute> customAttributes = new List<CustomAttribute>();
            customAttributes.Add(new CustomAttribute() { Key = "FirstName", Value = "Lucas" });
            customAttributes.Add(new CustomAttribute() { Key = "LastName", Value = "Scott" });
            var result = _converter.ConvertTo(customAttributes, typeof(string));
            Assert.IsNotNull(result);
        }
    }
}