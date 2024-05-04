using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class BoolTypeConverterTests
{
    private readonly BoolTypeConverter boolTypeConverter = new();

    [TestMethod]
    public void ConvertFromTest_Null_False()
    {
        var result = (bool)boolTypeConverter.ConvertFrom(null);
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ConvertFromTest_string_True()
    {
        var result = (bool)boolTypeConverter.ConvertFrom("true");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ConvertFromTest_String_True()
    {
        var result = (bool)boolTypeConverter.ConvertFrom("True");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void ConvertFromTest_int_False()
    {
        var result = (bool)boolTypeConverter.ConvertFrom(0);
        Assert.IsFalse(result);
    }
}