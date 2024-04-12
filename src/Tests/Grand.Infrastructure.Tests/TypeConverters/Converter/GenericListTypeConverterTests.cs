using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class GenericListTypeConverterTests
{
    private readonly GenericListTypeConverter<double> _doubleconverter = new();
    private readonly GenericListTypeConverter<int> _intconverter = new();
    private readonly GenericListTypeConverter<string> _stringconverter = new();

    [TestMethod]
    public void CanConvertFromTest_string_true()
    {
        Assert.IsTrue(_intconverter.CanConvertFrom(typeof(string)));
        Assert.IsTrue(_stringconverter.CanConvertFrom(typeof(string)));
        Assert.IsTrue(_doubleconverter.CanConvertFrom(typeof(string)));
    }

    [TestMethod]
    public void CanConvertFromTest_int_false()
    {
        Assert.IsFalse(_intconverter.CanConvertFrom(typeof(int)));
        Assert.IsFalse(_stringconverter.CanConvertFrom(typeof(int)));
        Assert.IsFalse(_doubleconverter.CanConvertFrom(typeof(int)));
    }

    [TestMethod]
    public void ConvertFromTest_List_string()
    {
        var mylist = (List<string>)_stringconverter.ConvertFrom("str1, str2");
        Assert.IsNotNull(mylist);
        Assert.IsTrue(mylist.Count > 0);
    }

    [TestMethod]
    public void ConvertFromTest_List_int()
    {
        var mylist = (List<int>)_intconverter.ConvertFrom("1, 2");
        Assert.IsNotNull(mylist);
        Assert.IsTrue(mylist.Count > 0);
    }

    [TestMethod]
    public void ConvertFromTest_list_double()
    {
        var mylist = (List<double>)_doubleconverter.ConvertFrom("1.1, 2");
        Assert.IsNotNull(mylist);
        Assert.IsTrue(mylist.Count == 2);
    }

    [TestMethod]
    public void ConvertToTest()
    {
        var str1 = _intconverter.ConvertTo(new List<int> { 1, 2 }, typeof(string));
        Assert.IsNotNull(str1);
        var str2 = _stringconverter.ConvertTo(new List<string> { "1", "2" }, typeof(string));
        Assert.IsNotNull(str2);
        var str3 = _doubleconverter.ConvertTo(new List<double> { 1, 2 }, typeof(string));
        Assert.IsNotNull(str3);
    }
}