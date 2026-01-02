using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Common.Tests.Extensions;

[TestClass]
public class TranslateExtensionsTests
{
    [TestMethod]
    public void GetTranslation_ReturnExpectedValue()
    {
        //prepare  ITranslationEntity
        var product = new Product {
            Name = "stname"
        };
        product.Locales.Add(new TranslationEntity { LanguageId = "PL", LocaleKey = "Name", LocaleValue = "PLName" });
        product.Locales.Add(new TranslationEntity { LanguageId = "UK", LocaleKey = "Name", LocaleValue = "UKName" });

        Assert.AreEqual("PLName", product.GetTranslation(c => c.Name, "PL"));
        Assert.AreEqual("UKName", product.GetTranslation(c => c.Name, "UK"));
        //if language dont exist return property value
        Assert.AreEqual("stname", product.GetTranslation(c => c.Name, "US"));
    }

    [TestMethod]
    public void GetTranslation_NullArgument_ThrowException()
    {
        Product product = null;
        Assert.ThrowsExactly<ArgumentNullException>(() => product.GetTranslation(c => c.Name, "PL"));
    }

    [TestMethod]
    public void GetTranslation_ExpressionUseMethod_ThrowException()
    {
        var product = new Product();
        Assert.ThrowsExactly<ArgumentException>(() =>
            product.GetTranslation(c => c.ParseRequiredProductIds().First(), "PL"));
    }

    [TestMethod]
    public void GetTranslationEnum_ReturnExpectedValue()
    {
        var translationServiceMock = new Mock<ITranslationService>();
        translationServiceMock
            .Setup(c => c.GetResource(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns("PLenum");
        var product = new Product {
            ManageInventoryMethodId = ManageInventoryMethod.ManageStock
        };
        var result = product.ManageInventoryMethodId.GetTranslationEnum(translationServiceMock.Object, "PL");
        Assert.AreEqual("PLenum", result);
    }

    [TestMethod]
    public void GetTranslationEnum_UseNotEnum_ThrowException()
    {
        var translationServiceMock = new Mock<ITranslationService>();
        var fake = new FakeStruct();
        Assert.ThrowsExactly<ArgumentException>(() => fake.GetTranslationEnum(translationServiceMock.Object, "PL"));
    }
    
    private struct FakeStruct;
}