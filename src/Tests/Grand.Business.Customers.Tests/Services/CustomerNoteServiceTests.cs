using Grand.Business.Customers.Services;
using Grand.Data;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services;

[TestClass]
public class CustomerNoteServiceTests
{
    private Mock<IMediator> _mediatorMock;
    private CustomerNoteService _noteService;
    private Mock<IRepository<CustomerNote>> _repositoryMock;

    [TestInitialize]
    public void Init()
    {
        _repositoryMock = new Mock<IRepository<CustomerNote>>();
        _mediatorMock = new Mock<IMediator>();
        _noteService = new CustomerNoteService(_repositoryMock.Object, _mediatorMock.Object);
    }

    [TestMethod]
    public async Task GetCustomerNoteTest()
    {
        await _noteService.GetCustomerNote("1");
        _repositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task GetCustomerNoteTest_Null()
    {
        await _noteService.GetCustomerNote("");
        _repositoryMock.Verify(c => c.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertCustomerNote_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _noteService.InsertCustomerNote(new CustomerNote());
        _repositoryMock.Verify(c => c.InsertAsync(It.IsAny<CustomerNote>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityInserted<CustomerNote>>(), default), Times.Once);
    }

    [TestMethod]
    public async Task DeleteCustomerNote_ValidArguments_InvokeRepositoryAndPublishEvent()
    {
        await _noteService.DeleteCustomerNote(new CustomerNote());
        _repositoryMock.Verify(c => c.DeleteAsync(It.IsAny<CustomerNote>()), Times.Once);
        _mediatorMock.Verify(c => c.Publish(It.IsAny<EntityDeleted<CustomerNote>>(), default), Times.Once);
    }
}