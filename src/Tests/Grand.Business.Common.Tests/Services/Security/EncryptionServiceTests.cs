using Grand.Business.Common.Services.Security;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Tests.Services.Security;

[TestClass]
public class EncryptionServiceTests
{
    private EncryptionService _encryptionService;

    [TestInitialize]
    public void Init()
    {
        _encryptionService = new EncryptionService();
    }

    [TestMethod]
    public void CreateSaltKey_ReturnOthersSaltWithSpecificSize()
    {
        var size = 32;
        var salt1 = _encryptionService.CreateSaltKey(size);
        var salt2 = _encryptionService.CreateSaltKey(size);
        Assert.AreNotEqual(salt1, salt2);
        Assert.IsTrue(Convert.FromBase64String(salt1).Length.Equals(size));
        Assert.IsTrue(Convert.FromBase64String(salt2).Length.Equals(size));
    }

    [TestMethod]
    public void CreatePasswordHash_InvokeWithTheSameArguments_ReturnTheSameValue()
    {
        const string password = "password";
        var salt = _encryptionService.CreateSaltKey(32);
        var hash1 = _encryptionService.CreatePasswordHash(password, salt);
        var hash2 = _encryptionService.CreatePasswordHash(password, salt);
        var hash3 = _encryptionService.CreatePasswordHash(password, salt, HashedPasswordFormat.SHA384);
        var hash4 = _encryptionService.CreatePasswordHash(password, salt, HashedPasswordFormat.SHA384);
        var hash5 = _encryptionService.CreatePasswordHash(password, salt, HashedPasswordFormat.SHA512);
        Assert.AreEqual(hash1, hash2);
        Assert.AreEqual(hash3, hash4);
        Assert.AreNotEqual(hash4, hash5);
    }
    
    [TestMethod]
    public void CreatePasswordHash_InvokeWithFixedSalt_ReturnExpectedHash()
    {
        // Arrange
        const string password = "password";
        const string salt = "tkpYgVYjK3P4hpLpqgY8popeQ26Ax8ZwyJaQ0F340yA=";
        const string expectedHash = "5FDEFB16C983C42DAF16FA9595EA61BADCA69558";
        
        // Act
        var actualHash = _encryptionService.CreatePasswordHash(password, salt);
        
        // Assert
        Assert.AreEqual(expectedHash, actualHash);
    }

    [TestMethod]
    public void EncryptText_TheSameKeyAndText_ReturnTheSameValues()
    {
        var privateKey = "secure key..............";
        var toEncrypte = "text to encrypte...";
        var encrypted1 = _encryptionService.EncryptText(toEncrypte, privateKey);
        var encrypted2 = _encryptionService.EncryptText(toEncrypte, privateKey);
        var encrypted3 = _encryptionService.EncryptText(toEncrypte, "7ecure key..............");
        Assert.AreEqual(encrypted2, encrypted1);
        Assert.AreNotEqual(encrypted2, encrypted3);
    }

    [TestMethod]
    public void EncryptText_InvalidPrivateKeyLength_ThrowException()
    {
        var privateKey = "secure key.";
        var toEncrypte = "text to encrypte...";
        Assert.ThrowsExactly<Exception>(() => _encryptionService.EncryptText(toEncrypte, privateKey));
    }

    [TestMethod]
    public void DecryptText_ReturnExpectedResult()
    {
        var privateKey = "secure key..............";
        var toEncrypte = "text to encrypte...";
        var encrypted1 = _encryptionService.EncryptText(toEncrypte, privateKey);
        var decrypt = _encryptionService.DecryptText(encrypted1, privateKey);
        Assert.AreEqual(decrypt, toEncrypte);
    }

    [TestMethod]
    public void DecryptText_InvalidPrivateKeyLength_ThrowException()
    {
        var privateKey = "secure key.";
        var toDescrypt = "gdfgdfgt45gfdfg";
        Assert.ThrowsExactly<Exception>(() => _encryptionService.DecryptText(toDescrypt, privateKey));
    }

    [TestMethod]
    public void HashPassword_ProducesSelfDescribingSaltedHash_ThatVerifies()
    {
        var hash1 = _encryptionService.HashPassword("password");
        var hash2 = _encryptionService.HashPassword("password");

        //self-describing PBKDF2 format and a random salt per call
        StringAssert.StartsWith(hash1, "PBKDF2$");
        Assert.AreNotEqual(hash1, hash2);

        Assert.IsTrue(_encryptionService.VerifyPassword("password", PasswordFormat.Hashed, hash1, string.Empty,
            HashedPasswordFormat.SHA1));
        Assert.IsFalse(_encryptionService.VerifyPassword("wrong", PasswordFormat.Hashed, hash1, string.Empty,
            HashedPasswordFormat.SHA1));
    }

    [TestMethod]
    public void VerifyPassword_VerifiesLegacyShaHash_WithoutRehashing()
    {
        var salt = _encryptionService.CreateSaltKey(16);
        var legacy = _encryptionService.CreatePasswordHash("password", salt, HashedPasswordFormat.SHA512);

        Assert.IsTrue(_encryptionService.VerifyPassword("password", PasswordFormat.Hashed, legacy, salt,
            HashedPasswordFormat.SHA512));
        Assert.IsFalse(_encryptionService.VerifyPassword("password", PasswordFormat.Hashed, legacy, salt,
            HashedPasswordFormat.SHA256));
    }

    [TestMethod]
    public void PasswordHashNeedsUpgrade_TrueForLegacyOrReversible_FalseForPbkdf2()
    {
        var pbkdf2 = _encryptionService.HashPassword("password");
        Assert.IsFalse(_encryptionService.PasswordHashNeedsUpgrade(PasswordFormat.Hashed, pbkdf2));

        Assert.IsTrue(_encryptionService.PasswordHashNeedsUpgrade(PasswordFormat.Hashed, "5FDEFB16C983"));
        Assert.IsTrue(_encryptionService.PasswordHashNeedsUpgrade(PasswordFormat.Clear, "password"));
        Assert.IsTrue(_encryptionService.PasswordHashNeedsUpgrade(PasswordFormat.Encrypted, "cipher"));
    }

    [TestMethod]
    public void PasswordHashNeedsUpgrade_TrueWhenEmbeddedIterationsBelowConfigured()
    {
        //hash created at the default cost, then verified against a service configured with a higher cost
        var weakHash = _encryptionService.HashPassword("password");
        var stronger = new EncryptionService(new SecurityConfig { PasswordHashIterations = 400_000 });

        Assert.IsTrue(stronger.PasswordHashNeedsUpgrade(PasswordFormat.Hashed, weakHash));
        Assert.IsFalse(stronger.PasswordHashNeedsUpgrade(PasswordFormat.Hashed, stronger.HashPassword("password")));
    }

    [TestMethod]
    public void IsHashedPassword_TrueOnlyForPbkdf2Format()
    {
        Assert.IsTrue(_encryptionService.IsHashedPassword(_encryptionService.HashPassword("password")));
        Assert.IsFalse(_encryptionService.IsHashedPassword(
            _encryptionService.CreatePasswordHash("password", _encryptionService.CreateSaltKey(16))));
        Assert.IsFalse(_encryptionService.IsHashedPassword("some-reversibly-encrypted-value"));
        Assert.IsFalse(_encryptionService.IsHashedPassword(null));
    }

    [TestMethod]
    public void VerifyPassword_Pbkdf2_FailsClosedOnMalformedHash()
    {
        //corrupted stored hashes must not throw on the auth path - they simply do not match
        foreach (var malformed in new[]
                 {
                     "PBKDF2$1$0$YQ==$YQ==", //zero iterations
                     "PBKDF2$1$-5$YQ==$YQ==", //negative iterations
                     "PBKDF2$1$210000$not-base64$YQ==", //invalid salt
                     "PBKDF2$1$210000$YQ==", //too few segments
                     "PBKDF2$1$210000$YQ==$" //empty hash
                 })
            Assert.IsFalse(_encryptionService.VerifyPassword("password", PasswordFormat.Hashed, malformed,
                string.Empty, HashedPasswordFormat.SHA1), $"should fail closed for: {malformed}");
    }

    [TestMethod]
    public void VerifyPassword_Pbkdf2_IsPepperSensitive()
    {
        var peppered = new EncryptionService(new SecurityConfig { PasswordHashKey = "server-pepper" });
        var hash = peppered.HashPassword("password");

        //same pepper verifies, missing/different pepper does not
        Assert.IsTrue(peppered.VerifyPassword("password", PasswordFormat.Hashed, hash, string.Empty,
            HashedPasswordFormat.SHA1));
        Assert.IsFalse(_encryptionService.VerifyPassword("password", PasswordFormat.Hashed, hash, string.Empty,
            HashedPasswordFormat.SHA1));
    }
}