using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Marketing.Interfaces.Newsletters;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.ExportImport;
using Grand.Business.System.Services.ExportImport;
using Grand.Domain.Catalog;
using Grand.Domain.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.System.Tests.Services.ExportImport
{
    [TestClass()]
    public class ExportManagerTests
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IPictureService> _pictureServiceMock;
        private ExportManager _exportManager;

        [TestInitialize()]
        public void TestInitialize()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _pictureServiceMock = new Mock<IPictureService>();
            _exportManager = new ExportManager(_pictureServiceMock.Object, _serviceProviderMock.Object);
        }

        [TestMethod]
        public void ExportBrandsToXlsx_ReturnFilledStreamWithCorrecctStructure()
        {
            var brands = new List<Brand>()
            {
                new Brand(){Id="id1",Name="brand1"},
                new Brand(){Id="id2",Name="brand2"}
            };

            var result = _exportManager.ExportBrandsToXlsx(brands);
            Assert.IsTrue(result.Length > 0);
            using (var ms = new MemoryStream(result))
            {
                XSSFWorkbook s = new XSSFWorkbook(ms);
                var sheet = s.GetSheet("Brand");
                var row1 = sheet.GetRow(0);
                var row2 = sheet.GetRow(1);
                Assert.IsNotNull(sheet);
                Assert.IsNotNull(row1);
                Assert.IsNotNull(row2);
                //row1 should contains header- property name 
                Assert.AreEqual(row1.GetCell(0).StringCellValue, "Id");
                Assert.AreEqual(row2.GetCell(0).StringCellValue, "id1");
            }
        }

        [TestMethod]
        public void ExportCollectionsToXlsx_ReturnFilledStreamWithCorrecctStructure()
        {
            var collections = new List<Collection>()
            {
                new Collection{Id="id1",Name="collection1"},
                new Collection{Id="id2",Name="collection2"}
            };

            var result = _exportManager.ExportCollectionsToXlsx(collections);
            Assert.IsTrue(result.Length > 0);
            using (var ms = new MemoryStream(result))
            {
                XSSFWorkbook s = new XSSFWorkbook(ms);
                var sheet = s.GetSheet("Collection");
                var row1 = sheet.GetRow(0);
                var row2 = sheet.GetRow(1);
                var row3 = sheet.GetRow(2);
                Assert.IsNotNull(sheet);
                Assert.IsNotNull(row1);
                Assert.IsNotNull(row2);
                //row1 should contains header- property name 
                Assert.AreEqual(row1.GetCell(0).StringCellValue, "Id");
                Assert.AreEqual(row1.GetCell(1).StringCellValue, "Name");
                Assert.AreEqual(row2.GetCell(0).StringCellValue, "id1");
                Assert.AreEqual(row2.GetCell(1).StringCellValue, "collection1");
                Assert.AreEqual(row3.GetCell(0).StringCellValue, "id2");
                Assert.AreEqual(row3.GetCell(1).StringCellValue, "collection2");
            }
        }

        [TestMethod]
        public void ExportCategoriesToXlsx_ReturnFilledStreamWithCorrecctStructure()
        {
            var categories = new List<Category>()
            {
                new Category{Id="id1",Name="name1"},
                new Category{Id="id2",Name="name2"}
            };

            var result = _exportManager.ExportCategoriesToXlsx(categories);
            Assert.IsTrue(result.Length > 0);
            using (var ms = new MemoryStream(result))
            {
                XSSFWorkbook s = new XSSFWorkbook(ms);
                var sheet = s.GetSheet("Category");
                var row1 = sheet.GetRow(0);
                var row2 = sheet.GetRow(1);
                var row3 = sheet.GetRow(2);
                Assert.IsNotNull(sheet);
                Assert.IsNotNull(row1);
                Assert.IsNotNull(row2);
                //row1 should contains header- property name 
                Assert.AreEqual(row1.GetCell(0).StringCellValue, "Id");
                Assert.AreEqual(row1.GetCell(1).StringCellValue, "Name");
                Assert.AreEqual(row2.GetCell(0).StringCellValue, "id1");
                Assert.AreEqual(row2.GetCell(1).StringCellValue, "name1");
                Assert.AreEqual(row3.GetCell(0).StringCellValue, "id2");
                Assert.AreEqual(row3.GetCell(1).StringCellValue, "name2");
            }
        }

        [TestMethod]
        public void ExportProductsToXlsx_ReturnFilledStreamWithCorrecctStructure()
        {
            var products = new List<Product>()
            {
                new Product{Id="id1",ProductTypeId=ProductType.Auction,Name="name1"},
                new Product{Id="id2",ProductTypeId=ProductType.SimpleProduct,Name="name2"}
            };

            var result = _exportManager.ExportProductsToXlsx(products);
            Assert.IsTrue(result.Length > 0);
            using (var ms = new MemoryStream(result))
            {
                XSSFWorkbook s = new XSSFWorkbook(ms);
                var sheet = s.GetSheet("Product");
                var row1 = sheet.GetRow(0);
                var row2 = sheet.GetRow(1);
                var row3 = sheet.GetRow(2);
                Assert.IsNotNull(sheet);
                Assert.IsNotNull(row1);
                Assert.IsNotNull(row2);
                //row1 should contains header- property name 
                Assert.AreEqual(row1.GetCell(0).StringCellValue, "Id");
                Assert.AreEqual(row1.GetCell(1).StringCellValue, "ProductTypeId");
                Assert.AreEqual(row2.GetCell(0).StringCellValue, "id1");
                Assert.AreEqual(row2.GetCell(1).StringCellValue, "Auction");
                Assert.AreEqual(row3.GetCell(0).StringCellValue, "id2");
                Assert.AreEqual(row3.GetCell(1).StringCellValue, "SimpleProduct");
            }
        }

        //Temporary disabled -- TODO
        /*
        [TestMethod]
        public void ExportNewsletterSubscribersToTxt_ReturnExpectedResult()
        {
            var list = new List<NewsLetterSubscription>()
            {
                new NewsLetterSubscription(){Email="a@gmail.com",Active=true,CreatedOnUtc=new DateTime(2021,04,12),StoreId="s1"},
                new NewsLetterSubscription(){Email="b@gmail.com",Active=false,CreatedOnUtc=new DateTime(2021,04,20),StoreId="s2"},
                new NewsLetterSubscription(){Email="c@gmail.com",Active=true,CreatedOnUtc=new DateTime(2021,04,21),StoreId="s3"},
            };

            var result = _exportManager.ExportNewsletterSubscribersToTxt(list);
            Assert.AreEqual("a@gmail.com,True,12.04.2021 00:00:00,s1,\r\nb@gmail.com,False,20.04.2021 00:00:00,s2,\r\nc@gmail.com,True,21.04.2021 00:00:00,s3,\r\n", result);
                            
        }
        */
    }
}
