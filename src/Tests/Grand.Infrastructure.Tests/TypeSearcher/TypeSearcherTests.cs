using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeSearcher;

[TestClass]
public class TypeSearcherTests
{
    private readonly TypeSearch.TypeSearcher _typeSearcher = new();

    [TestMethod]
    public void GetAssembliesTest()
    {
        Assert.IsTrue(_typeSearcher.GetAssemblies().Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_false()
    {
        Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>(false).Any());
    }

    [TestMethod]
    public void ClassesOfTypeTest_onlyConcreteClasses_true()
    {
        Assert.IsFalse(_typeSearcher.ClassesOfType<IWorkContext>().Any());
    }
}