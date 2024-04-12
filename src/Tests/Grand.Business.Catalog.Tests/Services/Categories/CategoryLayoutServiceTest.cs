using Grand.Business.Catalog.Services.Categories;
using Grand.Data;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Categories;

[TestClass]
public class CategoryLayoutServiceTest
{
    private Mock<ICacheBase> _cacheMock;
    private CategoryLayoutService _categoryLayoutService;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<CategoryLayout>> _repostioryMock;

    [TestInitialize]
    public void Init()
    {
        _repostioryMock = new Mock<IRepository<CategoryLayout>>();
        _cacheMock = new Mock<ICacheBase>();
        _mediatorMock = new Mock<IMediator>();
        _categoryLayoutService =
            new CategoryLayoutService(_repostioryMock.Object, _cacheMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetCategoryLayoutById_InvokeMethods()
    {
        await _categoryLayoutService.GetCategoryLayoutById("1");
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<CategoryLayout>>>()), Times.Once);
    }

    [TestMethod]
    public async Task GetAllCategoryLayouts_InvokeMethods()
    {
        await _categoryLayoutService.GetAllCategoryLayouts();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<CategoryLayout>>>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task DeleteCategoryLayout_ValidArguments_InvokeMethods()
    {
        await _categoryLayoutService.DeleteCategoryLayout(new CategoryLayout());
        _repostioryMock.Verify(c => c.DeleteAsync(It.IsAny<CategoryLayout>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CategoryLayout>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task InsertCategoryLayout_ValidArguments_InvokeMethods()
    {
        await _categoryLayoutService.InsertCategoryLayout(new CategoryLayout());
        _repostioryMock.Verify(c => c.InsertAsync(It.IsAny<CategoryLayout>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CategoryLayout>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task UpdateCategoryLayout_ValidArguments_InvokeMethods()
    {
        await _categoryLayoutService.UpdateCategoryLayout(new CategoryLayout());
        _repostioryMock.Verify(c => c.UpdateAsync(It.IsAny<CategoryLayout>()), Times.Once);
        _cacheMock.Verify(c => c.RemoveByPrefix(It.IsAny<string>(), true), Times.Exactly(1));
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<CategoryLayout>>(), default), Times.Once);
    }
}