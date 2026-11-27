using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.Features.Handlers.Products;
using Grand.Web.Features.Models.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Products;

[TestClass]
public class GetProductLayoutViewPathHandlerTests
{
    private readonly ProductLayout _default = new() { Id = "default", ViewPath = "ProductLayout.Simple" };
    private readonly ProductLayout _grouped = new() { Id = "grouped", ViewPath = "ProductLayout.Grouped" };
    private Mock<IProductLayoutService> _layoutServiceMock;
    private GetProductLayoutViewPathHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _layoutServiceMock = new Mock<IProductLayoutService>();
        _layoutServiceMock.Setup(x => x.GetProductLayoutById("grouped")).ReturnsAsync(_grouped);
        _layoutServiceMock.Setup(x => x.GetAllProductLayouts()).ReturnsAsync(new List<ProductLayout> { _default, _grouped });
        _handler = new GetProductLayoutViewPathHandler(_layoutServiceMock.Object);
    }

    [TestMethod]
    public async Task Handle_ProductWithLayout_ReturnsItsViewPath()
    {
        var path = await _handler.Handle(new GetProductLayoutViewPath { ProductLayoutId = "grouped" }, CancellationToken.None);

        Assert.AreEqual("ProductLayout.Grouped", path);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("deleted")]
    public async Task Handle_ProductWithoutAKnownLayout_ReturnsTheDefaultViewPath(string layoutId)
    {
        var path = await _handler.Handle(new GetProductLayoutViewPath { ProductLayoutId = layoutId }, CancellationToken.None);

        Assert.AreEqual("ProductLayout.Simple", path);
    }

    [TestMethod]
    public async Task Handle_NoLayoutAtAll_Throws()
    {
        _layoutServiceMock.Setup(x => x.GetAllProductLayouts()).ReturnsAsync(new List<ProductLayout>());

        await Assert.ThrowsExactlyAsync<Exception>(() =>
            _handler.Handle(new GetProductLayoutViewPath { ProductLayoutId = null }, CancellationToken.None));
    }
}
