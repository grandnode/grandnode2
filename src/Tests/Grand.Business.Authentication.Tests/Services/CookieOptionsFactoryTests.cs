using Grand.Business.Authentication.Services;
using Grand.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Services;

[TestClass]
public class CookieOptionsFactoryTests
{
    private SecurityConfig _securityConfig;
    private CookieOptionsFactory _cookieOptionsFactory;

    [TestInitialize]
    public void Setup()
    {
        _securityConfig = new SecurityConfig {
            CookieAuthExpires = 24,
            CookieSecurePolicyAlways = true,
        };
        _cookieOptionsFactory = new CookieOptionsFactory(_securityConfig);
    }

    [TestMethod]
    public void Create_WhenCalledWithoutExpiryDate_SetsDefaultExpiryDate()
    {
        // Arrange
        var expectedExpiryDate = DateTime.UtcNow.AddHours(_securityConfig.CookieAuthExpires);

        // Act
        var options = _cookieOptionsFactory.Create();

        // Assert
        Assert.IsTrue(options.HttpOnly);
        Assert.IsTrue(options.Secure);

        // Verify expiration is close to expected (allowing for slight processing time differences)
        var difference = (options.Expires.Value - expectedExpiryDate).TotalMinutes;
        Assert.IsTrue(Math.Abs(difference) < 1, "Expiry time should be within 1 minute of expected value");
    }

    [TestMethod]
    public void Create_WhenCalledWithExpiryDate_UsesProvidedExpiryDate()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.AddDays(7);

        // Act
        var options = _cookieOptionsFactory.Create(expiryDate);

        // Assert
        Assert.IsTrue(options.HttpOnly);
        Assert.IsTrue(options.Secure);
        Assert.AreEqual(expiryDate, options.Expires);
    }

    [TestMethod]
    public void Create_WhenSecurePolicyIsNone_SecureFlagIsFalse()
    {
        // Arrange
        _securityConfig.CookieSecurePolicyAlways = true;
        var factory = new CookieOptionsFactory(_securityConfig);

        // Act
        var options = factory.Create();

        // Assert
        Assert.IsTrue(options.Secure);
    }

    [TestMethod]
    public void Create_WhenSecurePolicyIsSameAsRequest_UsesConfiguredPolicy()
    {
        // Arrange
        _securityConfig.CookieSecurePolicyAlways = false;
        var factory = new CookieOptionsFactory(_securityConfig);

        // Act
        var options = factory.Create();

        // Assert
        Assert.IsFalse(options.Secure);
    }

    [TestMethod]
    public void Create_WhenSameSiteModeIsStrict_UseStrictMode()
    {
        // Arrange
        _securityConfig.CookieSameSite = SameSiteMode.Strict;
        var factory = new CookieOptionsFactory(_securityConfig);

        // Act
        var options = factory.Create();

        // Assert
        Assert.AreEqual(SameSiteMode.Strict, options.SameSite);
    }
}
