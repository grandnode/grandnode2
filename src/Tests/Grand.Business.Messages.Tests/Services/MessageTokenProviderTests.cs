using Grand.Business.Messages.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Tests.Services;

[TestClass]
public class MessageTokenProviderTests
{
    private MessageTokenProvider _provider;

    [TestInitialize]
    public void Init()
    {
        _provider = new MessageTokenProvider();
    }

    [TestMethod]
    public void GetListOfAllowedTokensTest()
    {
        var tokens = _provider.GetListOfAllowedTokens();
        Assert.IsTrue(tokens.Any());
    }

    [TestMethod]
    public void GetListOfCampaignAllowedTokensTest()
    {
        var tokens = _provider.GetListOfCampaignAllowedTokens();
        Assert.IsTrue(tokens.Any());
    }
}