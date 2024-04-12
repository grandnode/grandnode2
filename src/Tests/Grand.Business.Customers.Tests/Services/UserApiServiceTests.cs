using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class UserApiServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IRepository<UserApi>> _repositoryMock;
    private UserApiService _userApiService;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<UserApi>>();
        _mediatorMock = new Mock<IMediator>();
        _userApiService = new UserApiService(_repositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetUserByIdTest()
    {
        await _userApiService.GetUserById("");
        _repositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task InsertUserApi_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _userApiService.InsertUserApi(new UserApi());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<UserApi>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<UserApi>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteUserApi_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _userApiService.DeleteUserApi(new UserApi());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<UserApi>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<UserApi>>(), default), Times.Once);
    }


    [TestMethod]
    public async Task UpdateUserApi_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _userApiService.UpdateUserApi(new UserApi());
        _repositoryMock.Verify(c => c.UpdateAsync(It.IsAny<UserApi>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityUpdated<UserApi>>(), default), Times.Once);
    }
}