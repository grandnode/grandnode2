using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Knowledgebase;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class KnowledgebaseViewModelServiceTests
{
    private Mock<IKnowledgebaseService> _knowledgebaseServiceMock;
    private KnowledgebaseViewModelService _service;

    [TestInitialize]
    public void Setup()
    {
        _knowledgebaseServiceMock = new Mock<IKnowledgebaseService>();
        _service = new KnowledgebaseViewModelService(_knowledgebaseServiceMock.Object, Mock.Of<ISeNameService>());
    }

    private void Arrange(List<KnowledgebaseCategory> categories, List<KnowledgebaseArticle> articles)
    {
        _knowledgebaseServiceMock.Setup(x => x.GetKnowledgebaseCategories()).ReturnsAsync(categories);
        _knowledgebaseServiceMock.Setup(x => x.GetKnowledgebaseArticles(It.IsAny<string>())).ReturnsAsync(articles);
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_ListsCategoriesInTreeOrderThenRootArticles()
    {
        //root parent ids come as null and as empty string
        Arrange([
            new KnowledgebaseCategory { Id = "b", Name = "B", ParentCategoryId = "", DisplayOrder = 2 },
            new KnowledgebaseCategory { Id = "a1", Name = "A1", ParentCategoryId = "a", DisplayOrder = 1, Published = true },
            new KnowledgebaseCategory { Id = "a", Name = "A", ParentCategoryId = null, DisplayOrder = 1 },
            new KnowledgebaseCategory { Id = "a1x", Name = "A1X", ParentCategoryId = "a1", DisplayOrder = 0 }
        ], [
            new KnowledgebaseArticle { Id = "art-a", Name = "In A", ParentCategoryId = "a" },
            new KnowledgebaseArticle { Id = "art-root", Name = "Root", ParentCategoryId = "", DisplayOrder = 3, Published = true }
        ]);

        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel(1, 100);
        var list = nodes.ToList();

        Assert.AreEqual(5, total);
        CollectionAssert.AreEqual(new[] { "a", "a1", "a1x", "b", "art-root" }, list.Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(new[] { true, true, true, true, false }, list.Select(x => x.IsCategory).ToArray());
        CollectionAssert.AreEqual(new[] { "", "A", "A >> A1", "", "" }, list.Select(x => x.ParentCategory).ToArray());
        Assert.IsTrue(list[1].Published);
        Assert.AreEqual(3, list[4].DisplayOrder);
        Assert.IsTrue(list[4].Published);
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_PagesTheRows()
    {
        Arrange(Enumerable.Range(0, 5)
            .Select(i => new KnowledgebaseCategory { Id = $"c{i}", Name = $"C{i}", DisplayOrder = i }).ToList(), []);

        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel(2, 2);

        Assert.AreEqual(5, total);
        CollectionAssert.AreEqual(new[] { "c2", "c3" }, nodes.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_KeepsCategoriesOfAParentCycle()
    {
        Arrange([
            new KnowledgebaseCategory { Id = "x", Name = "X", ParentCategoryId = "y" },
            new KnowledgebaseCategory { Id = "y", Name = "Y", ParentCategoryId = "x" }
        ], []);

        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel(1, 10);

        Assert.AreEqual(2, total);
        CollectionAssert.AreEquivalent(new[] { "x", "y" }, nodes.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_ShowsAnOrphanedCategoryAtTheRoot()
    {
        Arrange([new KnowledgebaseCategory { Id = "o", Name = "O", ParentCategoryId = "deleted" }], []);

        var (nodes, _) = await _service.PrepareKnowledgebaseNodeGridModel(1, 10);

        Assert.AreEqual("", nodes.Single().ParentCategory);
    }
}
