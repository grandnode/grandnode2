using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Permissions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Extensions
{
    [TestClass()]
    public class TranslateExtensionsTests
    {
        [TestMethod()]
        public void GetTranslation_ReturnExpectedValue()
        {
            //prepare  ITranslationEntity
            var product = new Product();
            product.Name = "stname";
            product.Locales.Add(new Domain.Localization.TranslationEntity() { LanguageId = "PL", LocaleKey = "Name", LocaleValue = "PLName" });
            product.Locales.Add(new Domain.Localization.TranslationEntity() { LanguageId = "UK", LocaleKey = "Name", LocaleValue = "UKName" });

            Assert.AreEqual(product.GetTranslation(c => c.Name, "PL"), "PLName");
            Assert.AreEqual(product.GetTranslation(c => c.Name, "UK"), "UKName");
            //if language dont exist return property value
            Assert.AreEqual(product.GetTranslation(c => c.Name, "US"), "stname");
        }

        [TestMethod()]
        public void GetTranslation_NullArgument_ThrowException()
        {
            Product product = null;
            Assert.ThrowsException<ArgumentNullException>(() => product.GetTranslation(c => c.Name, "PL"));
        }

        [TestMethod()]
        public void GetTranslation_ExpressionUseMethod_ThrowException()
        {
            Product product = new Product();
            Assert.ThrowsException<ArgumentException>(() => product.GetTranslation(c => c.ParseRequiredProductIds().First(), "PL"));
        }

        [TestMethod()]
        public void GetTranslationEnum_ReturnExpectedValue()
        {
            var translationServiceMock = new Mock<ITranslationService>();
            translationServiceMock.Setup(c => c.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns("PLenum");
            Product product = new Product();
            product.ManageInventoryMethodId = ManageInventoryMethod.ManageStock;
            var result = product.ManageInventoryMethodId.GetTranslationEnum(translationServiceMock.Object, "PL");
            Assert.AreEqual(result, "PLenum");
        }

        [TestMethod()]
        public void GetTranslationEnum_UseNotEnum_ThrowException()
        {
            var translationServiceMock = new Mock<ITranslationService>();
            var fake = new FakeStruct();
            Assert.ThrowsException<ArgumentException>(() => fake.GetTranslationEnum(translationServiceMock.Object, "PL"));
        }

        [TestMethod()]
        public void GetTranslationPermissionName_ReturnExpectedValue()
        {
            var expectedValue = "PLpermision";
            var translationServiceMock = new Mock<ITranslationService>();
            translationServiceMock.Setup(c => c.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(expectedValue);
            Permission record = new Permission();
            record.SystemName = "sysname";
            var result = record.GetTranslationPermissionName(translationServiceMock.Object, "PL");
            Assert.AreEqual(result, expectedValue);
        }

        [TestMethod()]
        public void GetTranslationPermissionName_NullArgument_ThrowException()
        {
            var expectedValue = "PLpermision";
            var translationServiceMock = new Mock<ITranslationService>();
            translationServiceMock.Setup(c => c.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(expectedValue);
            Permission record = null;
            Assert.ThrowsException<ArgumentNullException>(() => record.GetTranslationPermissionName(translationServiceMock.Object, "PL"));
        }

        private struct FakeStruct
        {

        }
    }
}
