using Grand.Business.Catalog.Extensions;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Catalog.Tests.Extensions
{
    [TestClass()]
    public class ProductExtensionsTests
    {

        [TestMethod()]
        public void Can_parse_allowed_quantities()
        {
            //simple parsing witohut unallowed word-characters
            var product = new Product { AllowedQuantities = "1,3,42,1,dsad,123,d22,d,122223" };

            var result = product.ParseAllowedQuantities();
            Assert.AreEqual(6, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(3, result[1]);
            Assert.AreEqual(42, result[2]);
            Assert.AreEqual(1, result[3]);
            Assert.AreEqual(123, result[4]);
            Assert.AreEqual(122223, result[5]);
        }
    }
}
