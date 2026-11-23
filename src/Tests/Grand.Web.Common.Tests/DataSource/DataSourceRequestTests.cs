using Grand.Web.Common.DataSource;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Web.Common.Tests.DataSource;

[TestClass]
public class DataSourceRequestTests
{
    [TestMethod]
    public void PageSize_AboveTheLimit_Limited()
    {
        var request = new DataSourceRequest { PageSize = int.MaxValue };

        Assert.AreEqual(DataSourceRequest.MaxPageSize, request.PageSize);
    }

    [TestMethod]
    public void PageSize_WithinTheLimit_Kept()
    {
        var request = new DataSourceRequest { PageSize = 50 };

        Assert.AreEqual(50, request.PageSize);
    }

    [TestMethod]
    public void PageAndPageSize_BelowOne_One()
    {
        var request = new DataSourceRequest { Page = -3, PageSize = 0 };

        Assert.AreEqual(1, request.Page);
        Assert.AreEqual(1, request.PageSize);
    }
}