using Grand.Domain.Security;
using Grand.Infrastructure.TypeConverters.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeConverters.Converter;

[TestClass]
public class RefreshTokenTypeConverterTests
{
    private readonly RefreshTokenTypeConverter refreshTokenTypeConverter = new();

    [TestMethod]
    public void CanConvertFromTest_string()
    {
        Assert.IsTrue(refreshTokenTypeConverter.CanConvertFrom(typeof(string)));
    }

    [TestMethod]
    public void CanConvertFromTest_decimal()
    {
        Assert.IsFalse(refreshTokenTypeConverter.CanConvertFrom(typeof(decimal)));
    }

    [TestMethod]
    public void ConvertFromTest_NotNull()
    {
        var refreshToken = refreshTokenTypeConverter.ConvertFrom(
            "{\"RefreshId\":\"eed7fe9a-a920-4212-be3a-043e31737615\",\"Token\":\"aa7c8eda-9d79-4a70-8936-ee03bcc97daf\",\"ValidTo\":\"2022-09-22T15:42:52.8392185Z\",\"IsActive\":true}");
        Assert.IsNotNull(refreshToken);
    }

    [TestMethod]
    public void ConvertFromTest_Null()
    {
        var refreshToken = refreshTokenTypeConverter.ConvertFrom("sdf");
        Assert.IsNull(refreshToken);
    }

    [TestMethod]
    public void ConvertToTest_NotNull()
    {
        var refreshToken = new RefreshToken {
            RefreshId = Guid.NewGuid().ToString(),
            Token = Guid.NewGuid().ToString(),
            IsActive = true,
            ValidTo = DateTime.UtcNow.AddDays(1)
        };
        var str = refreshTokenTypeConverter.ConvertTo(refreshToken, typeof(string));
        Assert.IsNotNull(str);
        Assert.IsInstanceOfType(str, typeof(string));
    }

    [TestMethod]
    public void ConvertToTest_decimal_Exception()
    {
        var refreshToken = new RefreshToken {
            RefreshId = Guid.NewGuid().ToString(),
            Token = Guid.NewGuid().ToString(),
            IsActive = true,
            ValidTo = DateTime.UtcNow.AddDays(1)
        };

        Assert.ThrowsException<NotSupportedException>(() =>
            _ = refreshTokenTypeConverter.ConvertTo(refreshToken, typeof(decimal)));
    }
}