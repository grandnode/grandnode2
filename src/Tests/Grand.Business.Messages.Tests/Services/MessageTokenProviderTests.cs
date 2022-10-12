using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Messages.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Messages.Services.Tests
{
    [TestClass()]
    public class MessageTokenProviderTests
    {
        private MessageTokenProvider _provider;

        [TestInitialize()]
        public void Init()
        {
            _provider = new MessageTokenProvider();
        }

        [TestMethod()]
        public void GetListOfAllowedTokensTest()
        {
            var tokens = _provider.GetListOfAllowedTokens();
            Assert.IsTrue(tokens.Any());
        }
        [TestMethod()]
        public void GetListOfCampaignAllowedTokensTest()
        {
            var tokens = _provider.GetListOfCampaignAllowedTokens();
            Assert.IsTrue(tokens.Any());
        }
    }
}