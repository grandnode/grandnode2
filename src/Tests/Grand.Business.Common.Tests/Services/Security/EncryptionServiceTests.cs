using Grand.Business.Common.Services.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Services.Security
{
    [TestClass()]
    public class EncryptionServiceTests
    {
        private EncryptionService _encryptionService;

        [TestInitialize]
        public void Init()
        {
            _encryptionService = new EncryptionService();
        }

        [TestMethod()]
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
        public void CreatePasswordHash_InvokeWithTheSameArguemtns_ReturnTheSameValue()
        {
            var password = "password";
            var salt = _encryptionService.CreateSaltKey(32);
            var hash1 = _encryptionService.CreatePasswordHash(password, salt);
            var hash2 = _encryptionService.CreatePasswordHash(password, salt);
            var hash3 = _encryptionService.CreatePasswordHash(password, salt,Domain.Customers.HashedPasswordFormat.SHA384);
            var hash4 = _encryptionService.CreatePasswordHash(password, salt,Domain.Customers.HashedPasswordFormat.SHA384);
            var hash5 = _encryptionService.CreatePasswordHash(password, salt,Domain.Customers.HashedPasswordFormat.SHA512);
            Assert.AreEqual(hash1, hash2);
            Assert.AreEqual(hash3, hash4);
            Assert.AreNotEqual(hash4, hash5);
        }

        [TestMethod()]
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

        [TestMethod()]
        public void EncryptText_InvalidPrivateKeyLength_ThrowException()
        {
            var privateKey = "secure key.";
            var toEncrypte = "text to encrypte...";
            Assert.ThrowsException<Exception>(() => _encryptionService.EncryptText(toEncrypte, privateKey));
        }

        [TestMethod()]
        public void DecryptText_ReturnExpectedResult()
        {
            var privateKey = "secure key..............";
            var toEncrypte = "text to encrypte...";
            var encrypted1 = _encryptionService.EncryptText(toEncrypte, privateKey);
            var decrypt = _encryptionService.DecryptText(encrypted1, privateKey);
            Assert.AreEqual(decrypt, toEncrypte);
        }

        [TestMethod()]
        public void DecryptText_InvalidPrivateKeyLength_ThrowException()
        {
            var privateKey = "secure key.";
            var toDescrypt = "gdfgdfgt45gfdfg";
            Assert.ThrowsException<Exception>(() => _encryptionService.DecryptText(toDescrypt, privateKey));
        }
    }
}
