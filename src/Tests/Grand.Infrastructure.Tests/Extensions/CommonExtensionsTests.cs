using Grand.Infrastructure.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.Extensions
{
    [TestClass()]
    public class CommonExtensionsTests
    {
        [TestMethod()]
        public void ModifyQueryStringTest_url_withoutparam()
        {
            var url = new Uri("http://google.com");
            var param = "param";
            var value = "value";
            var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
            Assert.AreEqual(query, $"http://google.com/?{param}={value}");
        }
        [TestMethod()]
        public void ModifyQueryStringTest_url_with1param()
        {
            var url = new Uri("http://google.com?param=aaa");
            var param = "param";
            var value = "value";
            var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
            Assert.AreEqual(query, $"http://google.com/?{param}={value}");
        }
        [TestMethod()]
        public void ModifyQueryStringTest_url_with2params()
        {
            var url = new Uri("http://google.com?param=bbb&param2=cccc");
            var param = "param";
            var value = "value";
            var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
            Assert.AreEqual(query, $"http://google.com/?param2=cccc&{param}={value}");
        }
        [TestMethod()]
        public void ModifyQueryStringTest_url_with3params()
        {
            var url = new Uri("http://google.com?vvv=azaz&param=bbb&param2=cccc");
            var param = "param";
            var value = "value";
            var query = CommonExtensions.ModifyQueryString(url.ToString(), param, value);
            Assert.AreEqual(query, $"http://google.com/?vvv=azaz&param2=cccc&{param}={value}");
        }

    }
}