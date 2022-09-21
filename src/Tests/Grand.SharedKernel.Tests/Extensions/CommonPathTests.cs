using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.SharedKernel.Extensions.Tests
{
    [TestClass()]
    public class CommonPathTests
    {
        public CommonPathTests()
        {
            CommonPath.BaseDirectory = "base";
            CommonPath.WebHostEnvironment = "www";
        }

        [TestMethod()]
        public void MapPathTest()
        {
            var path = CommonPath.MapPath("path");
            Assert.AreEqual(@"base\path", path);
        }


        [TestMethod()]
        public void WebMapPathTest()
        {
            var path = CommonPath.WebMapPath("path");
            Assert.AreEqual(@"www\path", path);
        }

        [TestMethod()]
        public void WebHostMapPathTest()
        {
            var path = CommonPath.WebHostMapPath("path");
            Assert.AreEqual(@"www\path", path);
        }
    }
}