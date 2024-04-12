using Grand.Business.Common.Services.Directory;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Domain.History;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class HistoryServiceTests
{
    private IHistoryService _historyService;
    private Mock<IRepository<HistoryObject>> _mockHistoryRepository;

    [TestInitialize]
    public void Init()
    {
        _mockHistoryRepository = new Mock<IRepository<HistoryObject>>();
        _historyService = new HistoryService(_mockHistoryRepository.Object);
    }

    [TestMethod]
    public void SaveObject_NullObject_ThrowException()
    {
        Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await _historyService.SaveObject<Product>(null),
            "entity");
    }

    [TestMethod]
    public async Task SaveObject_InvokeRepositoryWithCorrectObject()
    {
        var product = new Product { Id = "1" };
        await _historyService.SaveObject(product);
        _mockHistoryRepository.Verify(c => c.InsertAsync(It.Is<HistoryObject>(h => h.Object.Id.Equals(product.Id))),
            Times.Once);
    }
}