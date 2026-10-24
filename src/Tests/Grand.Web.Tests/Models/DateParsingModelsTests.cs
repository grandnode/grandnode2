using Grand.Web.Models.Blogs;
using Grand.Web.Models.Customer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Tests.Models;

[TestClass]
public class DateParsingModelsTests
{
    [TestMethod]
    [DataRow(2024, 2, 29, true)]
    [DataRow(2023, 2, 29, false)]
    [DataRow(2024, 13, 1, false)]
    [DataRow(2024, 4, 31, false)]
    [DataRow(0, 1, 1, false)]
    public void ParseDateOfBirth_ReturnsADateOnlyWhenItExists(int year, int month, int day, bool valid)
    {
        var register = new RegisterModel { DateOfBirthYear = year, DateOfBirthMonth = month, DateOfBirthDay = day };
        var info = new CustomerInfoModel { DateOfBirthYear = year, DateOfBirthMonth = month, DateOfBirthDay = day };

        var expected = valid ? new DateTime(year, month, day) : (DateTime?)null;
        Assert.AreEqual(expected, register.ParseDateOfBirth());
        Assert.AreEqual(expected, info.ParseDateOfBirth());
    }

    [TestMethod]
    public void ParseDateOfBirth_MissingPart_ReturnsNull()
    {
        Assert.IsNull(new RegisterModel { DateOfBirthYear = 2000, DateOfBirthMonth = 1 }.ParseDateOfBirth());
        Assert.IsNull(new CustomerInfoModel { DateOfBirthYear = 2000, DateOfBirthDay = 1 }.ParseDateOfBirth());
    }

    [TestMethod]
    [DataRow("2024-5", 2024, 5)]
    [DataRow("2024-13", 0, 0)]
    [DataRow("2024-0", 0, 0)]
    [DataRow("abc-5", 0, 0)]
    [DataRow("2024", 0, 0)]
    [DataRow("", 0, 0)]
    public void GetParsedMonth_ReturnsTheFirstDayOfAValidMonth(string month, int year, int expectedMonth)
    {
        var model = new BlogPagingFilteringModel { Month = month };

        var expected = year > 0 ? new DateTime(year, expectedMonth, 1) : (DateTime?)null;
        Assert.AreEqual(expected, model.GetParsedMonth());
    }
}
