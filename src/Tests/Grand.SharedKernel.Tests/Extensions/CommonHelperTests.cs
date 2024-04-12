using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class CommonHelperTests
{
    [TestMethod]
    [DataRow("email")]
    [DataRow("email@email")]
    [DataRow("email@email@email.pl")]
    public void EnsureSubscriberEmailOrThrowTest_ThrowException(string email)
    {
        Assert.ThrowsException<GrandException>(() => CommonHelper.EnsureSubscriberEmailOrThrow(email));
    }

    [TestMethod]
    [DataRow("email@email.com")]
    [DataRow("sample.email@sample.com")]
    public void EnsureSubscriberEmailOrThrowTest_Success(string email)
    {
        Assert.IsTrue(CommonHelper.EnsureSubscriberEmailOrThrow(email) == email);
    }

    [TestMethod]
    [DataRow("email")]
    [DataRow("email@email")]
    [DataRow("email@email@email.pl")]
    public void IsValidEmailTest_False(string email)
    {
        Assert.IsFalse(CommonHelper.IsValidEmail(email));
    }

    [TestMethod]
    [DataRow("email@email.com")]
    [DataRow("sample.email@sample.com")]
    public void IsValidEmailTest_True(string email)
    {
        Assert.IsTrue(CommonHelper.IsValidEmail(email));
    }

    [TestMethod]
    [DataRow(5)]
    [DataRow(7)]
    public void GenerateRandomDigitCodeTest(int length)
    {
        var result = CommonHelper.GenerateRandomDigitCode(length);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length == length);
    }

    [TestMethod]
    [DataRow(5, 7)]
    [DataRow(6, 10)]
    public void GenerateRandomIntegerTest_True(int min, int max)
    {
        var result = CommonHelper.GenerateRandomInteger(min, max);

        Assert.IsTrue(result >= min);
        Assert.IsTrue(result <= max);
    }

    [TestMethod]
    [DataRow(10, 1)]
    public void GenerateRandomIntegerTest_ThrowException(int min, int max)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CommonHelper.GenerateRandomInteger(min, max));
    }

    [TestMethod]
    public void EnsureMaximumLengthTest_Value_3_True()
    {
        var str = "value";
        var max = 3;
        string post = null;
        Assert.IsTrue(CommonHelper.EnsureMaximumLength(str, max, post) == "val");
    }

    [TestMethod]
    public void EnsureMaximumLengthTest_Value_10_True()
    {
        var str = "value";
        var max = 10;
        string post = null;
        Assert.IsTrue(CommonHelper.EnsureMaximumLength(str, max, post) == "value");
    }

    [TestMethod]
    public void EnsureMaximumLengthTest_Value_2_Post_True()
    {
        var str = "0123456789000";
        var max = 10;
        var post = "...";
        Assert.IsTrue(CommonHelper.EnsureMaximumLength(str, max, post) == "0123456...");
    }

    [TestMethod]
    [DataRow("value", 1, "...")]
    public void EnsureMaximumLengthTest_ThrowException(string str, int max, string post)
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => CommonHelper.EnsureMaximumLength(str, max, post));
    }

    [TestMethod]
    public void ArraysEqualTest_True()
    {
        Assert.IsTrue(CommonHelper.ArraysEqual(["a1", "a2"], ["a1", "a2"]));
    }

    [TestMethod]
    public void ArraysEqualTest_False()
    {
        Assert.IsFalse(CommonHelper.ArraysEqual(["a1", "a2"], ["a2", "a3"]));
    }

    [TestMethod]
    public void ToTest_True()
    {
        object obj = "sample";
        Assert.IsTrue(obj == CommonHelper.To(obj, typeof(string)));
    }

    [TestMethod]
    public void ToTest_ThrowException()
    {
        object obj = "sample";
        Assert.ThrowsException<ArgumentException>(() => CommonHelper.To(obj, typeof(decimal)));
    }

    [TestMethod]
    public void ConvertEnumTest()
    {
        var value = SampleEnum.Test0;
        Assert.IsTrue(CommonHelper.ConvertEnum(value) == "Test0");
    }

    [TestMethod]
    public void GetDifferenceInYearsTest()
    {
        Assert.IsTrue(CommonHelper.GetDifferenceInYears(new DateTime(2010, 01, 01), new DateTime(2020, 01, 01)) == 10);
        Assert.IsTrue(CommonHelper.GetDifferenceInYears(new DateTime(2010, 02, 01), new DateTime(2020, 01, 01)) == 9);
        Assert.IsTrue(CommonHelper.GetDifferenceInYears(new DateTime(2011, 01, 02), new DateTime(2020, 01, 01)) == 8);
    }

    [TestMethod]
    public void ToTest_T()
    {
        object obj = "sample";
        Assert.IsTrue(obj.ToString() == CommonHelper.To<string>(obj));
    }

    [TestMethod]
    public void ToCultureInfoTest()
    {
        object obj = "sample";
        Assert.IsTrue(obj == CommonHelper.To(obj, typeof(string), CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow("test")]
    [DataRow(null)]
    public void EnsureNotNullTest(string text)
    {
        var result = CommonHelper.EnsureNotNull(text);
        Assert.IsNotNull(result);
    }
}