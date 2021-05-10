using Grand.Business.Common.Extensions;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Common.Tests.Extensions
{
    [TestClass()]
    public class SeoExtensionsTests
    {
        [TestMethod()]
        public void SeoExtensionsTest()
        {
            var charConversions = "ä:a;ö:o;ü:u;т:t;е:e;с:s;т:t";

            //german letters with diacritics
            Assert.AreEqual("testaou", SeoExtensions.GenerateSlug("testäöü", true, false, false, charConversions));
            Assert.AreEqual("test", SeoExtensions.GenerateSlug("testäöü", false, false, false));
            //russian letters           
            Assert.AreEqual("testtest", SeoExtensions.GenerateSlug("testтест", true, false, false, charConversions));
            Assert.AreEqual("test", SeoExtensions.GenerateSlug("testтест", false, false, false));

            Assert.AreEqual("test", SeoExtensions.GenerateSlug("testтест", false, false, false));
            Assert.AreEqual("testтест", SeoExtensions.GenerateSlug("testтест", false, true, false));

            //other
            Assert.AreEqual("abcdefghijklmnopqrstuvwxyz1234567890", SeoExtensions.GenerateSlug("abcdefghijklmnopqrstuvwxyz1234567890", false, false, false));

            Assert.AreEqual("test-test", SeoExtensions.GenerateSlug("test test", false, false, false));
            Assert.AreEqual("test-test", SeoExtensions.GenerateSlug("test     test", false, false, false));
        }

        [TestMethod()]
        public void GetSeName_ReturnExpectedValue()
        {
            var product = new Product();
            product.SeName = "se-name";
            //if lang id null , return global se name
            Assert.AreEqual("se-name", product.GetSeName<Product>(null));
            product.Locales.Add(new Domain.Localization.TranslationEntity() { LocaleKey = "SeName", LocaleValue = "se-name-1", LanguageId = "1" });
            product.Locales.Add(new Domain.Localization.TranslationEntity() { LocaleKey = "SeName", LocaleValue = "se-name-2", LanguageId = "2" });
            Assert.AreEqual("se-name-1", product.GetSeName<Product>("1"));
            Assert.AreEqual("se-name-2", product.GetSeName<Product>("2"));
        }

        [TestMethod()]
        public void GenerateSlug_ReturnExpectedResult()
        {
            Assert.AreEqual(SeoExtensions.GenerateSlug("iphone10plus", false, false, false), "iphone10plus");
            Assert.AreEqual(SeoExtensions.GenerateSlug("iphone 10 plus", false, false, false), "iphone-10-plus");
            Assert.AreEqual(SeoExtensions.GenerateSlug("iphOnE 10 Plus", false, false, false), "iphone-10-plus");
        }
    }
}
