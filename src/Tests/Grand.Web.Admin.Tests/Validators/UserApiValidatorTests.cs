using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Validators.Customers;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

#pragma warning disable CS0618 //API users stay supported until removed

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class UserApiValidatorTests
{
    private const string Registered = "Admin.System.UserApi.Email.Registered";
    private Mock<IRequestTranslationService> _translations = null!;
    private Mock<ICustomerService> _customers = null!;
    private Mock<IUserApiService> _users = null!;

    public UserApiValidatorTests()
    {
        _translations = new Mock<IRequestTranslationService>();
        _translations.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        _customers = new Mock<ICustomerService>();
        _customers.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new Customer { Active = true });
        _users = new Mock<IUserApiService>();
        _users.Setup(x => x.GetUserByEmail("taken@shop.com")).ReturnsAsync(new UserApi { Id = "u1" });
    }

    private UserApiCreateValidator CreateValidator() =>
        new(new List<IValidatorConsumer<UserApiCreateModel>>(), _translations.Object, _customers.Object, _users.Object);

    private UserApiValidator EditValidator() =>
        new(new List<IValidatorConsumer<UserApiModel>>(), _translations.Object, _customers.Object, _users.Object);

    [TestMethod]
    public async Task Create_EmailOfAnotherApiUser_IsInvalid()
    {
        var result = await CreateValidator().ValidateAsync(new UserApiCreateModel { Email = "taken@shop.com", Password = "p" });
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == Registered));
    }

    [TestMethod]
    public async Task Create_FreeEmail_IsValid()
    {
        var result = await CreateValidator().ValidateAsync(new UserApiCreateModel { Email = "free@shop.com", Password = "p" });
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Edit_OwnEmail_IsValid()
    {
        var result = await EditValidator().ValidateAsync(new UserApiModel { Id = "u1", Email = "taken@shop.com" });
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task Edit_EmailOfAnotherApiUser_IsInvalid()
    {
        var result = await EditValidator().ValidateAsync(new UserApiModel { Id = "u2", Email = "taken@shop.com" });
        Assert.IsTrue(result.Errors.Any(e => e.ErrorMessage == Registered));
    }
}
