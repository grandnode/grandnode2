using Grand.Business.Authentication.Services;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Security;
using Grand.Domain.Stores;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.TypeConverters.Converter;
using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.ComponentModel;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class RefreshTokenServiceTests
{
    private RefreshTokenService _service;
    private Mock<ICustomerService> _customerServiceMock;

    [TestInitialize]
    public void Init()
    {
        _customerServiceMock = new Mock<ICustomerService>();
        _service = new RefreshTokenService(_customerServiceMock.Object, new FrontendAPIConfig {
            SecretKey = "JWTRefreshTokenHIGHsecuredPasswordVVVp1OH7Xzyr",
            ValidIssuer = "http://localhost:4200",
            ValidAudience = "http://localhost:4200",
            ValidateLifetime = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = false,
            ExpiryInMinutes = 1440,
            RefreshTokenExpiryInMinutes = 1440,
            Enabled = true,
            ValidateIssuer = false
        });
    }

    [TestMethod]
    public void GenerateRefreshTokenTest()
    {
        //Act
        var result = _service.GenerateRefreshToken();
        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task SaveRefreshTokenToCustomerTest()
    {
        //Arrange
        _customerServiceMock.Setup(c =>
            c.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
        var token = Guid.NewGuid().ToString();
        //Act
        var result = await _service.SaveRefreshTokenToCustomer(new Customer(), token);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(token, result.Token);
    }

    [TestMethod]
    public async Task GetCustomerRefreshTokenTest()
    {
        //Arrange
        TypeDescriptor.AddAttributes(typeof(RefreshToken),
            new TypeConverterAttribute(typeof(RefreshTokenTypeConverter)));

        var valueStr = CommonHelper.To<string>(new RefreshToken { Token = "123" });
        var customer = new Customer();
        customer.UserFields.Add(new UserField() { Key = SystemCustomerFieldNames.RefreshToken, Value = valueStr, StoreId = "" });

        //Act
        var result = await _service.GetCustomerRefreshToken(customer);
        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("123", result.Token);
    }
}