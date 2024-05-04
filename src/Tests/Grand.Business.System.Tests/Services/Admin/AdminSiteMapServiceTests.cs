using Grand.Business.System.Services.Admin;
using Grand.Data;
using Grand.Domain.Admin;
using Grand.Infrastructure.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.System.Tests.Services.Admin;

[TestClass]
public class AdminSiteMapServiceTests
{
    private Mock<ICacheBase> _cacheMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<AdminSiteMap>> _repositoryMock;
    private AdminSiteMapService _service;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<AdminSiteMap>>();
        _cacheMock = new Mock<ICacheBase>();
        _mediatorMock = new Mock<IMediator>();
        _service = new AdminSiteMapService(_repositoryMock.Object, _cacheMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetSiteMap_InvokeExpectedMethods()
    {
        _cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<AdminSiteMap>>>>()));
        var result = await _service.GetSiteMap();
        _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<List<AdminSiteMap>>>>()), Times.Once);
    }
}