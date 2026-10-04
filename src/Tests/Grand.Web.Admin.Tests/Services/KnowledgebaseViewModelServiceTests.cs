using Grand.Business.Core.Interfaces.Cms;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Domain.Knowledgebase;
using Grand.Web.AdminShared.Models.Knowledgebase;
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

    private async Task<List<KnowledgebaseNodeGridModel>> Level(string parentId)
    {
        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel(parentId, 1, 100);
        var list = nodes.ToList();
        Assert.AreEqual(list.Count, total);
        return list;
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_ListsTheChildrenOfOneLevel()
    {
        //root parent ids come as null and as empty string
        Arrange([
            new KnowledgebaseCategory { Id = "b", Name = "B", ParentCategoryId = "", DisplayOrder = 2 },
            new KnowledgebaseCategory { Id = "a1", Name = "A1", ParentCategoryId = "a", DisplayOrder = 1, Published = true },
            new KnowledgebaseCategory { Id = "a", Name = "A", ParentCategoryId = null, DisplayOrder = 1 },
            new KnowledgebaseCategory { Id = "a1x", Name = "A1X", ParentCategoryId = "a1", DisplayOrder = 0 }
        ], [
            new KnowledgebaseArticle { Id = "art-a2", Name = "In A 2", ParentCategoryId = "a", DisplayOrder = 2 },
            new KnowledgebaseArticle { Id = "art-a", Name = "In A", ParentCategoryId = "a", DisplayOrder = 1 },
            new KnowledgebaseArticle { Id = "art-root", Name = "Root", ParentCategoryId = "", DisplayOrder = 3, Published = true },
            new KnowledgebaseArticle { Id = "art-a1x", Name = "Deep", ParentCategoryId = "a1x" }
        ]);

        var root = await Level(null);
        CollectionAssert.AreEqual(new[] { "a", "b", "art-root" }, root.Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(new[] { true, true, false }, root.Select(x => x.IsCategory).ToArray());
        //a: subcategory a1 and two articles; b: nothing to expand
        CollectionAssert.AreEqual(new[] { 3, 0, 0 }, root.Select(x => x.ChildCount).ToArray());
        Assert.AreEqual(3, root[2].DisplayOrder);
        Assert.IsTrue(root[2].Published);

        //the empty string asks for the root too
        CollectionAssert.AreEqual(new[] { "a", "b", "art-root" }, (await Level("")).Select(x => x.Id).ToArray());

        //subcategories first, then articles, each by display order
        var a = await Level("a");
        CollectionAssert.AreEqual(new[] { "a1", "art-a", "art-a2" }, a.Select(x => x.Id).ToArray());
        Assert.IsTrue(a[0].Published);
        Assert.AreEqual(1, a[0].ChildCount);

        var a1 = await Level("a1");
        CollectionAssert.AreEqual(new[] { "a1x" }, a1.Select(x => x.Id).ToArray());
        Assert.AreEqual(1, a1[0].ChildCount);

        CollectionAssert.AreEqual(new[] { "art-a1x" }, (await Level("a1x")).Select(x => x.Id).ToArray());
        Assert.AreEqual(0, (await Level("b")).Count);
        Assert.AreEqual(0, (await Level("unknown")).Count);
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_PagesTheRows()
    {
        Arrange(Enumerable.Range(0, 5)
            .Select(i => new KnowledgebaseCategory { Id = $"c{i}", Name = $"C{i}", DisplayOrder = i }).ToList(), []);

        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel(null, 2, 2);

        Assert.AreEqual(5, total);
        CollectionAssert.AreEqual(new[] { "c2", "c3" }, nodes.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_PagesTheChildrenOfACategory()
    {
        Arrange([new KnowledgebaseCategory { Id = "p", Name = "P" }],
            Enumerable.Range(0, 12).Select(i => new KnowledgebaseArticle {
                Id = $"a{i}", Name = $"A{i}", ParentCategoryId = "p", DisplayOrder = i
            }).ToList());

        var (nodes, total) = await _service.PrepareKnowledgebaseNodeGridModel("p", 2, 10);

        Assert.AreEqual(12, total);
        CollectionAssert.AreEqual(new[] { "a10", "a11" }, nodes.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_ListsEachCategoryOfAParentCycleOnce()
    {
        //x and y are each other's parent; z hangs under the cycle
        Arrange([
            new KnowledgebaseCategory { Id = "z", Name = "Z", ParentCategoryId = "x" },
            new KnowledgebaseCategory { Id = "x", Name = "X", ParentCategoryId = "y" },
            new KnowledgebaseCategory { Id = "y", Name = "Y", ParentCategoryId = "x" }
        ], []);

        var root = await Level(null);
        Assert.AreEqual(1, root.Count);
        var seen = new List<string>();
        var pending = new Queue<string>(root.Select(x => x.Id));
        seen.AddRange(pending);
        while (pending.Count > 0)
            foreach (var child in await Level(pending.Dequeue()))
            {
                seen.Add(child.Id);
                pending.Enqueue(child.Id);
            }

        //every category once, and expanding ends
        CollectionAssert.AreEquivalent(new[] { "x", "y", "z" }, seen);
        CollectionAssert.AreEquivalent(new[] { "y", "z" }, (await Level("x")).Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task PrepareKnowledgebaseNodeGridModel_ShowsWhatHangsUnderADeletedCategoryAtTheRoot()
    {
        Arrange([new KnowledgebaseCategory { Id = "o", Name = "O", ParentCategoryId = "deleted" }],
            [new KnowledgebaseArticle { Id = "art-o", Name = "Orphan", ParentCategoryId = "deleted" }]);

        CollectionAssert.AreEqual(new[] { "o", "art-o" }, (await Level(null)).Select(x => x.Id).ToArray());
    }
}
