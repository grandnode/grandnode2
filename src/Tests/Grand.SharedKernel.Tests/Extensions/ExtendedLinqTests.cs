using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.SharedKernel.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.SharedKernel.Tests;

namespace Grand.SharedKernel.Extensions.Tests
{
    [TestClass()]
    public class ExtendedLinqTests
    {
        [TestMethod()]
        public void CartesianProductTest()
        {
            var sampleList = new List<SampleObject>() { 
                new SampleObject() { Id = "1", Name = "name1" }, 
                new SampleObject() { Id = "1", Name = "name2" },
                new SampleObject() { Id = "3", Name = "name3" }
            };

            var cartesianProduct = sampleList.GroupBy(x=>x.Id).CartesianProduct().ToList();
            Assert.AreEqual(2, cartesianProduct.Count);
        }
    }
}