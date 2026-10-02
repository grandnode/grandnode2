using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Common;
using Grand.Web.Features.Handlers.Common;
using Grand.Web.Features.Models.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Common;

[TestClass]
public class GetRobotsTextFileHandlerTests
{
    private Mock<IRobotsTxtService> _serviceMock;
    private GetRobotsTextFileHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _serviceMock = new Mock<IRobotsTxtService>();
        _handler = new GetRobotsTextFileHandler(_serviceMock.Object);
    }

    [TestMethod]
    public async Task StoreHasItsOwn_ServesIt()
    {
        _serviceMock.Setup(s => s.GetRobotsTxt("store-2")).ReturnsAsync(new RobotsTxt { Text = "store" });
        _serviceMock.Setup(s => s.GetRobotsTxt("")).ReturnsAsync(new RobotsTxt { Text = "global" });

        Assert.AreEqual("store", await _handler.Handle(new GetRobotsTextFile { StoreId = "store-2" }, CancellationToken.None));
    }

    [TestMethod]
    public async Task StoreHasNone_ServesTheAllStoresOne()
    {
        _serviceMock.Setup(s => s.GetRobotsTxt("store-2")).ReturnsAsync((RobotsTxt)null);
        _serviceMock.Setup(s => s.GetRobotsTxt("")).ReturnsAsync(new RobotsTxt { Text = "global" });

        Assert.AreEqual("global", await _handler.Handle(new GetRobotsTextFile { StoreId = "store-2" }, CancellationToken.None));
    }

    [TestMethod]
    public async Task NoneAtAll_ServesEmpty()
    {
        Assert.AreEqual("", await _handler.Handle(new GetRobotsTextFile { StoreId = "store-2" }, CancellationToken.None));
    }
}
