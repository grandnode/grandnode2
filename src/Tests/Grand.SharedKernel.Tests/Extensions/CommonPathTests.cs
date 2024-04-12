using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class CommonPathTests
{
    public CommonPathTests()
    {
        CommonPath.BaseDirectory = "base";
        CommonPath.WebHostEnvironment = "www";
    }

    [TestMethod]
    public void MapPathTest()
    {
        var path = CommonPath.MapPath("path");
        Assert.AreEqual(Path.Combine("base", "path"), path);
    }


    [TestMethod]
    public void WebMapPathTest()
    {
        var path = CommonPath.WebMapPath("path");
        Assert.AreEqual(Path.Combine("www", "path"), path);
    }

    [TestMethod]
    public void WebHostMapPathTest()
    {
        var path = CommonPath.WebHostMapPath("path");
        Assert.AreEqual(Path.Combine("www", "path"), path);
    }
}