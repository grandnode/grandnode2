using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Customers;
using Grand.Business.Customers.Queries.Handlers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Queries.Handlers;

[TestClass]
public class GetPasswordIsExpiredQueryHandlerTests
{
    private Mock<IGroupService> _groupServiceMock;
    private OidcLoginConfig _loginConfig;
    private GetPasswordIsExpiredQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _groupServiceMock = new Mock<IGroupService>();
        _loginConfig = new OidcLoginConfig();
        handler = new GetPasswordIsExpiredQueryHandler(_groupServiceMock.Object,
            new CustomerSettings { PasswordLifetime = 10 }, _loginConfig);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Assert
        var cr = new CustomerGroup { Active = true, EnablePasswordLifetime = true };
        var cgs = new List<CustomerGroup> { cr };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));

        var passwordIsExpiredQuery = new GetPasswordIsExpiredQuery();
        var customer = new Customer { Email = "email@email.com" };
        customer.Groups.Add(cr.Id);
        passwordIsExpiredQuery.Customer = customer;
        //Act
        var result = await handler.Handle(passwordIsExpiredQuery, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Handle_ProviderOnly_ExpiredPassword_IsNotExpired()
    {
        //Arrange
        _loginConfig.SignInMode = OidcSignInMode.ProviderOnly;
        var cr = new CustomerGroup { Active = true, EnablePasswordLifetime = true };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup> { cr }));
        var customer = new Customer { Email = "email@email.com", PasswordChangeDateUtc = DateTime.UtcNow.AddDays(-30) };
        customer.Groups.Add(cr.Id);
        //Act
        var result = await handler.Handle(new GetPasswordIsExpiredQuery { Customer = customer }, CancellationToken.None);
        //Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Handle_Mixed_ExpiredPassword_IsExpired()
    {
        //Arrange
        _loginConfig.SignInMode = OidcSignInMode.Mixed;
        var cr = new CustomerGroup { Active = true, EnablePasswordLifetime = true };
        _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
            .Returns(Task.FromResult<IList<CustomerGroup>>(new List<CustomerGroup> { cr }));
        var customer = new Customer { Email = "email@email.com", PasswordChangeDateUtc = DateTime.UtcNow.AddDays(-30) };
        customer.Groups.Add(cr.Id);
        //Act
        var result = await handler.Handle(new GetPasswordIsExpiredQuery { Customer = customer }, CancellationToken.None);
        //Assert
        Assert.IsTrue(result);
    }
}
