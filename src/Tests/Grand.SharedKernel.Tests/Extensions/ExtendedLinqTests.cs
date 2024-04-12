using Grand.SharedKernel.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.SharedKernel.Tests.Extensions;

[TestClass]
public class ExtendedLinqTests
{
    [TestMethod]
    public void CartesianProductTest()
    {
        var sampleList = new List<SampleObject> {
            new() { Id = "1", Name = "name1" },
            new() { Id = "1", Name = "name2" },
            new() { Id = "3", Name = "name3" }
        };

        var cartesianProduct = sampleList.GroupBy(x => x.Id).CartesianProduct().ToList();
        Assert.AreEqual(2, cartesianProduct.Count);
    }

    [TestMethod]
    public void ContainsAnyTest_True()
    {
        var sampleList = new List<int> { 1, 2, 3, 4 };
        Assert.IsTrue(sampleList.ContainsAny(new List<int> { 2, 3 }));
    }

    [TestMethod]
    public void ContainsAnyTest2_True()
    {
        var sampleList = new List<int> { 1, 2, 3, 4 };
        Assert.IsTrue(sampleList.ContainsAny(new List<int> { 2, 5 }));
    }

    [TestMethod]
    public void ContainsAnyTest_False()
    {
        var sampleList = new List<int> { 1, 2, 3, 4 };
        Assert.IsFalse(sampleList.ContainsAny(new List<int> { 5, 6 }));
    }
}