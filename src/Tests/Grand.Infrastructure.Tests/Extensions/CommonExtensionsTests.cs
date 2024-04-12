using Grand.Infrastructure.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Extensions;

[TestClass]
public class CommonExtensionsTests
{
    [TestMethod]
    public void ModifyQueryStringTest_url_withoutparam()
    {
        var url = new Uri("http://google.com/computers");
        var param = "param";
        var value = "value";
        var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
        Assert.AreEqual(query, $"/computers?{param}={value}");
    }

    [TestMethod]
    public void ModifyQueryStringTest_url_with1param()
    {
        var url = new Uri("http://google.com/computers?param=aaa");
        var param = "param";
        var value = "value";
        var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
        Assert.AreEqual(query, $"/computers?{param}={value}");
    }

    [TestMethod]
    public void ModifyQueryStringTest_url_with2params()
    {
        var url = new Uri("http://google.com/computers?param=bbb&param2=cccc");
        var param = "param";
        var value = "value";
        var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
        Assert.AreEqual(query, $"/computers?param2=cccc&{param}={value}");
    }

    [TestMethod]
    public void ModifyQueryStringTest_url_with3params()
    {
        var url = new Uri("http://google.com/computers?vvv=azaz&param=bbb&param2=cccc");
        var param = "param";
        var value = "value";
        var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
        Assert.AreEqual(query, $"/computers?vvv=azaz&param2=cccc&{param}={value}");
    }
}