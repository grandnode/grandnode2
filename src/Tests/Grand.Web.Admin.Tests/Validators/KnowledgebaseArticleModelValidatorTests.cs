using Grand.Business.Core.Interfaces.Cms;
using Grand.Domain.Knowledgebase;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Knowledgebase;
using Grand.Web.AdminShared.Validators.Knowledgebase;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Validators;

[TestClass]
public class KnowledgebaseArticleModelValidatorTests
{
    private KnowledgebaseArticleModelValidator _validator = null!;

    [TestInitialize]
    public void Init()
    {
        var translations = new Mock<IRequestTranslationService>();
        translations.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key);
        var knowledgebaseService = new Mock<IKnowledgebaseService>();
        knowledgebaseService.Setup(x => x.GetKnowledgebaseCategory("c1"))
            .ReturnsAsync(new KnowledgebaseCategory { Id = "c1" });
        _validator = new KnowledgebaseArticleModelValidator(new List<IValidatorConsumer<KnowledgebaseArticleModel>>(),
            translations.Object, knowledgebaseService.Object);
    }

    [TestMethod]
    public async Task ExistingCategory_IsValid()
    {
        var result = await _validator.ValidateAsync(new KnowledgebaseArticleModel { Name = "a", ParentCategoryId = "c1" });
        Assert.IsTrue(result.IsValid);
    }

    [TestMethod]
    public async Task UnknownCategory_IsInvalid()
    {
        var result = await _validator.ValidateAsync(new KnowledgebaseArticleModel { Name = "a", ParentCategoryId = "missing" });
        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.MustExist",
            result.Errors.Single().ErrorMessage);
    }

    [TestMethod]
    public async Task EmptyCategory_GivesOnlyTheRequiredMessage()
    {
        var result = await _validator.ValidateAsync(new KnowledgebaseArticleModel { Name = "a", ParentCategoryId = "" });
        Assert.AreEqual("Admin.Content.Knowledgebase.KnowledgebaseArticle.Fields.ParentCategoryId.Required",
            result.Errors.Single().ErrorMessage);
    }
}
