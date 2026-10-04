using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Configuration;
using Grand.Module.Api.Commands.Models.Common;
using Grand.Module.Api.Controllers;
using Grand.Module.Api.DTOs;
using Grand.Module.Api.Models.Common;
using Grand.Mediator;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;

namespace Grand.Module.Api.Tests.Controllers;

[TestClass]
public class TokenWebControllerTests
{
    private const string StoreId = "store-1";

    private Mock<ICustomerService> _customerServiceMock;
    private Mock<IMediator> _mediatorMock;
    private Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private GenerateTokenWebCommand _capturedCommand;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _mediatorMock = new Mock<IMediator>();
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();

        _refreshTokenServiceMock.Setup(r => r.GenerateRefreshToken()).Returns("rt");
        _refreshTokenServiceMock.Setup(r => r.SaveRefreshTokenToCustomer(It.IsAny<Customer>(), It.IsAny<string>()))
            .ReturnsAsync(new RefreshToken { RefreshId = "rid" });
        _mediatorMock.Setup(m => m.Send(It.IsAny<GenerateTokenWebCommand>(), It.IsAny<CancellationToken>()))
            .Callback((IRequest<string> req, CancellationToken _) => _capturedCommand = req as GenerateTokenWebCommand)
            .ReturnsAsync("jwt");
    }

    private TokenWebController CreateController(bool perStore)
    {
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(s => s.CurrentStore).Returns(new Store { Id = StoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(c => c.StoreContext).Returns(storeContext.Object);

        return new TokenWebController(
            _customerServiceMock.Object, _mediatorMock.Object, contextAccessor.Object,
            _refreshTokenServiceMock.Object, new Mock<IAntiforgery>().Object,
            new FrontendAPIConfig { Enabled = true },
            new CustomerConfig { RegisterCustomersPerStore = perStore });
    }

    [TestMethod]
    public async Task Login_PerStoreOn_ScopesLookupToStore_AndAddsCustomerIdClaim()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId))
            .ReturnsAsync(new Customer { Id = "c1" });
        var controller = CreateController(perStore: true);

        var result = await controller.Login(new LoginWebModel { Email = "u@x.com", Password = "p" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), StoreId), Times.Once);
        Assert.IsNotNull(_capturedCommand);
        Assert.AreEqual("c1", _capturedCommand.Claims["CustomerId"]);
    }

    [TestMethod]
    public async Task Login_PerStoreOff_UsesGlobalLookup()
    {
        _customerServiceMock.Setup(c => c.GetCustomerByEmail(It.IsAny<string>(), ""))
            .ReturnsAsync(new Customer { Id = "c1" });
        var controller = CreateController(perStore: false);

        var result = await controller.Login(new LoginWebModel { Email = "u@x.com", Password = "p" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), ""), Times.Once);
    }

    [TestMethod]
    public async Task Refresh_WithCustomerIdClaim_ResolvesById()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("CustomerId", "c1") }));
        _refreshTokenServiceMock.Setup(r => r.GetPrincipalFromToken("access")).Returns(principal);
        var customer = new Customer { Id = "c1", Email = "u@x.com" };
        _customerServiceMock.Setup(c => c.GetCustomerById("c1")).ReturnsAsync(customer);
        _refreshTokenServiceMock.Setup(r => r.GetCustomerRefreshToken(customer))
            .ReturnsAsync(new RefreshToken { Token = "rt", ValidTo = DateTime.UtcNow.AddDays(1), RefreshId = "rid" });
        var controller = CreateController(perStore: true);

        var result = await controller.Refresh(new TokenDto { AccessToken = "access", RefreshToken = "rt" });

        Assert.IsInstanceOfType(result, typeof(OkObjectResult));
        _customerServiceMock.Verify(c => c.GetCustomerById("c1"), Times.Once);
        _customerServiceMock.Verify(c => c.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
