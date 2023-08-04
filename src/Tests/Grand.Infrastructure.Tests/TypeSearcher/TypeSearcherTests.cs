using Grand.Infrastructure.Caching;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Infrastructure.Tests.TypeSearcher
{
    [TestClass()]
    public class TypeSearcherTests
    {
        private TypeSearch.TypeSearcher _typeSearcher;

        public TypeSearcherTests()
        {
            _typeSearcher = new TypeSearch.TypeSearcher();
        }

        [TestMethod()]
        public void GetAssembliesTest()
        {
            Assert.IsTrue(_typeSearcher.GetAssemblies().Any());
        }

        [TestMethod()]
        public void ClassesOfTypeTest_onlyConcreteClasses_false()
        {
            Assert.IsTrue(_typeSearcher.ClassesOfType<ICacheBase>(false).Any());
        }
        [TestMethod()]
        public void ClassesOfTypeTest_onlyConcreteClasses_true()
        {
            Assert.IsFalse(_typeSearcher.ClassesOfType<IWorkContext>(true).Any());
        }
    }
}