using Grand.Domain.Common;
using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class CustomAttributeListTypeConverterTests
{
    private readonly CustomAttributeListTypeConverter _converter = new();

    [TestMethod]
    public void CanConvertFromTest_True()
    {
        Assert.IsTrue(_converter.CanConvertFrom(typeof(string)));
    }

    [TestMethod]
    public void CanConvertFromTest_False()
    {
        Assert.IsFalse(_converter.CanConvertFrom(typeof(decimal)));
    }

    [TestMethod]
    public void ConvertFromTest_NotNull()
    {
        var str = "[{\"Key\":\"FirstName\",\"Value\":\"Lucas\"},{\"Key\":\"LastName\",\"Value\":\"Scott\"}]";
        var converted = _converter.ConvertFrom(str);
        Assert.IsNotNull(converted);
    }

    [TestMethod]
    public void ConvertFromTest_Null()
    {
        var converted = _converter.ConvertFrom("test");
        Assert.IsNull(converted);
    }

    [TestMethod]
    public void ConvertToTest()
    {
        List<CustomAttribute> customAttributes = [
            new CustomAttribute { Key = "FirstName", Value = "Lucas" },
            new CustomAttribute { Key = "LastName", Value = "Scott" }
        ];
        var result = _converter.ConvertTo(customAttributes, typeof(string));
        Assert.IsNotNull(result);
    }
}