using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Utilities.Tests
{
    [TestClass()]
    public class TokenTests
    {
        private Token _token;
        [TestInitialize]
        public void Init()
        {
            _token = new Token("key", "value");
        }
        [TestMethod()]
        public void TokenTest_key()
        {
            Assert.IsTrue("key" == _token.Key);
        }

        [TestMethod()]
        public void TokenTest_value()
        {
            Assert.IsTrue("value" == _token.Value);
        }

        [TestMethod()]
        public void ToStringTest()
        {
            Assert.IsTrue("key: value" == _token.ToString());
        }
    }
}