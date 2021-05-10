using Grand.Business.Marketing.Extensions;
using Grand.Domain.Knowledgebase;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Tests.Extensions
{
    [TestClass()]
    public class KnowledgebaseExtensionsTests
    {
        private List<KnowledgebaseCategory> _allCategories;

        [TestInitialize()]
        public void Init()
        {
            _allCategories = new List<KnowledgebaseCategory>()
            {
                new KnowledgebaseCategory(){Id="1",Name="category1",ParentCategoryId="2"},
                new KnowledgebaseCategory(){Id="2",Name="category2",ParentCategoryId="3"},
                new KnowledgebaseCategory(){Id="3",Name="category3",ParentCategoryId="4"},
                new KnowledgebaseCategory(){Id="4",Name="category4"},
                new KnowledgebaseCategory(){Id="5",Name="category5"},
                new KnowledgebaseCategory(){Id="6",Name="category6"},
            };
        }


        [TestMethod()]
        public void GetFormattedBreadCrumb_ReturnExpectedValue()
        {
            var category = _allCategories.First();
            var result = category.GetFormattedBreadCrumb(_allCategories);
            var category2 = new KnowledgebaseCategory() { Id = "2", Name = "category2", ParentCategoryId = "3" };
            Assert.AreEqual("category4 >> category3 >> category2 >> category1", result);
            Assert.AreEqual("category4 // category3 // category2 // category1", category.GetFormattedBreadCrumb(_allCategories,separator:"//"));
            Assert.AreEqual("category4 >> category3 >> category2", category2.GetFormattedBreadCrumb(_allCategories));
        }
    }
}
