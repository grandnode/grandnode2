using Grand.Business.Catalog.Services.Products;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Tests.Service.Products
{
    [TestClass()]
    public class ProductAttributeParserTests
    {
        private ProductAttributeParser _parrser;
        private List<CustomAttribute> customAtr;

        [TestInitialize()]
        public void Init()
        {
            _parrser = new ProductAttributeParser();
            customAtr = new List<CustomAttribute>()
            {
                new CustomAttribute(){Key="key1",Value="value1" },
                new CustomAttribute(){Key="key2",Value="value2" },
                new CustomAttribute(){Key="key3",Value="value3" },
                new CustomAttribute(){Key="key4",Value="value4" },
            };
        }

        [TestMethod()]
        public void ParseProductAttributeMappings_ReturnExpectedValues()
        {
            var product = new Product();
            product.ProductAttributeMappings.Add(new ProductAttributeMapping() { Id="key1"});
            product.ProductAttributeMappings.Add(new ProductAttributeMapping() { Id="key2"});
            var result =  _parrser.ParseProductAttributeMappings(product, customAtr);

            Assert.IsTrue(result.Any(c => c.Id.Equals("key1")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("key2")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("key3")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("key4")));
        }

        [TestMethod()]
        public void ParseProductAttributeMappings_ReturnEmptyList()
        {
            var product = new Product();
            product.ProductAttributeMappings.Add(new ProductAttributeMapping() { Id = "key10" });
            product.ProductAttributeMappings.Add(new ProductAttributeMapping() { Id = "key12" });
            var result = _parrser.ParseProductAttributeMappings(product, customAtr);
            Assert.IsTrue(result.Count == 0);
        }

        [TestMethod()]
        public void ParseProductAttributeValues_ReturnExpectedValues()
        {
            var product = new Product();
            var mapping1 = new ProductAttributeMapping() { Id = "key1" };
            mapping1.ProductAttributeValues.Add(new ProductAttributeValue() { Id = "value1" });
            product.ProductAttributeMappings.Add(mapping1);
            var mapping2 = new ProductAttributeMapping() { Id = "key2" };
            mapping2.ProductAttributeValues.Add(new ProductAttributeValue() { Id = "value2" });
            product.ProductAttributeMappings.Add(mapping2);
            var result = _parrser.ParseProductAttributeValues(product,customAtr);

            Assert.IsTrue(result.Any(c => c.Id.Equals("value1")));
            Assert.IsTrue(result.Any(c => c.Id.Equals("value2")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("value3")));
            Assert.IsFalse(result.Any(c => c.Id.Equals("value4")));
        }

        [TestMethod()]
        public void ParseValues_ReturnExpectedValues()
        {
            var result = _parrser.ParseValues(customAtr, "key1");
            Assert.IsTrue(result.Any(c => c.Equals("value1")));
        }

        [TestMethod()]
        public void AddProductAttribute_ReturnExpectedValues()
        {
            var result = _parrser.AddProductAttribute(customAtr, new ProductAttributeMapping() { Id = "key6" }, "value6");
            Assert.IsTrue(result.Count==5);
            Assert.IsTrue(result.Last().Value.Equals("value6"));
        }

        [TestMethod()]
        public void RemoveProductAttribute_ReturnExpectedValues()
        {
            var result = _parrser.RemoveProductAttribute(customAtr, new ProductAttributeMapping() { Id = "key1" });
            Assert.IsTrue(result.Count == 3);
            Assert.IsFalse(result.Any(c=>c.Key.Equals("key1")));
        }
    }
}
