using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.SharedKernel.Extensions;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products;

[TestClass]
public class ProductServiceTests
{
    private IAclService _aclServiceMock;
    private MemoryCacheBase _cacheBase;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _productRepository;
    private ProductService _productService;
    private Mock<IWorkContext> _workContextMock;

    [TestInitialize]
    public void InitializeTests()
    {
        CommonPath.BaseDirectory = "";

        _productRepository = new MongoDBRepositoryTest<Product>();
        _workContextMock = new Mock<IWorkContext>();
        _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Store { Id = "" });
        _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());
        _mediatorMock = new Mock<IMediator>();

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetProductArchByIdQuery>(), default))
            .Returns(Task.FromResult(new Product()));

        _aclServiceMock = new AclService(new AccessControlConfig());
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, _productRepository, _workContextMock.Object,
            _mediatorMock.Object, _aclServiceMock);
    }

    private async Task insertSampleProducts()
    {
        await _productRepository.InsertAsync(new Product
            { Id = "1", Published = true, VisibleIndividually = true, ShowOnHomePage = true });
        await _productRepository.InsertAsync(new Product
            { Published = true, VisibleIndividually = true, ShowOnHomePage = true });
        await _productRepository.InsertAsync(new Product
            { Published = true, VisibleIndividually = true, BestSeller = true });
        await _productRepository.InsertAsync(new Product
            { Published = true, VisibleIndividually = true, BestSeller = true });
        await _productRepository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        await _productRepository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        await _productRepository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        await _productRepository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        await _productRepository.InsertAsync(new Product
            { Published = true, VisibleIndividually = true, Sku = "test123" });
        await _productRepository.InsertAsync(new Product
            { Published = true, VisibleIndividually = true, ParentGroupedProductId = "1" });

        var product = new Product { Published = true, VisibleIndividually = true };
        product.AppliedDiscounts.Add("1");
        product.ProductCategories.Add(new ProductCategory { CategoryId = "1" });
        product.ProductAttributeMappings.Add(new ProductAttributeMapping { ProductAttributeId = "1" });
        await _productRepository.InsertAsync(product);
    }

    [TestMethod]
    public async Task GetAllProductsDisplayedOnHomePageTest()
    {
        //Arrange 
        await insertSampleProducts();

        //Act
        var result = await _productService.GetAllProductsDisplayedOnHomePage();

        //Assert
        Assert.IsTrue(result.Count == 2);
    }

    [TestMethod]
    public async Task GetAllProductsDisplayedOnBestSellerTest()
    {
        //Arrange 
        await insertSampleProducts();

        //Act
        var result = await _productService.GetAllProductsDisplayedOnBestSeller();

        //Assert
        Assert.IsTrue(result.Count == 2);
    }

    [TestMethod]
    public async Task GetProductByIdTest_NotNull()
    {
        //Arrange 
        await insertSampleProducts();

        //Act
        var result = await _productService.GetProductById("1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetProductByIdTest_Null()
    {
        //Arrange 
        await insertSampleProducts();

        //Act
        var result = await _productService.GetProductById("x");

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetProductByIdIncludeArchTest()
    {
        //Act
        var result = await _productService.GetProductByIdIncludeArch("2");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetProductsByIdsTest()
    {
        await insertSampleProducts();
        //Act
        var result = await _productService.GetProductsByIds(["1"]);

        //Assert
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task GetProductsByDiscountTest()
    {
        //Arrange
        await insertSampleProducts();

        //Act
        var result = await _productService.GetProductsByDiscount("1");

        //Assert
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task InsertProductTest()
    {
        //Arrange 
        var product = new Product { Id = "1" };
        await _productService.InsertProduct(product);

        //Act
        var result = await _productService.GetProductById("1");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task UpdateProductTest()
    {
        //Arrange 
        var product = new Product { Id = "103", Name = "test" };
        await _productService.InsertProduct(product);

        //Act
        product.Name = "test2";
        await _productService.UpdateProduct(product);
        var result = await _productService.GetProductById("103");

        //Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Name == "test2");
    }

    [TestMethod]
    public async Task DeleteProductTest()
    {
        //Arrange 
        var product = new Product { Id = "1" };
        await _productService.InsertProduct(product);

        //Act
        await _productService.DeleteProduct(product);
        var result = await _productService.GetProductById("1", true);

        //Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UnPublishProductTest()
    {
        //Arrange 
        var product = new Product { Id = "100", Published = true };
        await _productService.InsertProduct(product);

        //Act
        await _productService.UnPublishProduct(product);
        var result = await _productService.GetProductById("100");

        //Assert
        Assert.IsNotNull(result);
        Assert.IsFalse(result.Published);
    }

    [TestMethod]
    public async Task GetCategoryProductNumberTest()
    {
        //Arrange 
        await insertSampleProducts();
        //Act
        var result = _productService.GetCategoryProductNumber(new Customer(), new[] { "1" });

        //Assert
        Assert.IsTrue(result == 1);
    }

    [TestMethod]
    [Ignore]
    public void SearchProductsTest()
    {
    }

    [TestMethod]
    public async Task GetProductsByProductAtributeIdTest()
    {
        //Arrange 
        await insertSampleProducts();
        //Act
        var result = await _productService.GetProductsByProductAttributeId("1");

        //Assert
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task GetAssociatedProductsTest()
    {
        //Arrange 
        await insertSampleProducts();
        //Act
        var result = await _productService.GetAssociatedProducts("1");

        //Assert
        Assert.IsTrue(result.Count > 0);
    }

    [TestMethod]
    public async Task GetProductBySkuTest()
    {
        //Arrange 
        await insertSampleProducts();
        //Act
        var result = await _productService.GetProductBySku("test123");

        //Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task UpdateProductFieldTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        await _productService.UpdateProductField(product, x => x.Name, "test");

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task IncrementProductFieldTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        await _productService.IncrementProductField(product, x => x.Viewed, 1);
        await _productService.IncrementProductField(product, x => x.Viewed, 1);
        await _productService.IncrementProductField(product, x => x.Viewed, 1);

        var result = await _productService.GetProductById(product.Id, true);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.Viewed);
    }

    [TestMethod]
    public async Task UpdateAssociatedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        product.ParentGroupedProductId = "2";
        await _productService.UpdateAssociatedProduct(product);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("2", result.ParentGroupedProductId);
    }

    [TestMethod]
    public async Task InsertRelatedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        await _productService.InsertRelatedProduct(new RelatedProduct { ProductId2 = "2" }, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.RelatedProducts.Count);
    }

    [TestMethod]
    public async Task DeleteRelatedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var relatedproducts = new RelatedProduct { ProductId2 = "2" };
        await _productService.InsertRelatedProduct(relatedproducts, product.Id);

        //Act
        await _productService.DeleteRelatedProduct(relatedproducts, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.RelatedProducts.Count);
    }

    [TestMethod]
    public async Task UpdateRelatedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var relatedproducts = new RelatedProduct { ProductId2 = "2" };
        await _productService.InsertRelatedProduct(relatedproducts, product.Id);

        //Act
        relatedproducts.DisplayOrder = 10;
        await _productService.UpdateRelatedProduct(relatedproducts, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.RelatedProducts.Count);
        Assert.AreEqual(10, result.RelatedProducts.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task InsertSimilarProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        await _productService.InsertSimilarProduct(new SimilarProduct {
            ProductId1 = product.Id,
            ProductId2 = "2"
        });

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.SimilarProducts.Count);
    }

    [TestMethod]
    public async Task UpdateSimilarProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var similarProduct = new SimilarProduct { ProductId1 = product.Id, ProductId2 = "2" };
        await _productService.InsertSimilarProduct(similarProduct);

        //Act
        similarProduct.DisplayOrder = 10;
        await _productService.UpdateSimilarProduct(similarProduct);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.SimilarProducts.Count);
        Assert.AreEqual(10, result.SimilarProducts.FirstOrDefault().DisplayOrder);
    }

    [TestMethod]
    public async Task DeleteSimilarProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var similarProduct = new SimilarProduct { ProductId1 = product.Id, ProductId2 = "2" };
        await _productService.InsertSimilarProduct(similarProduct);

        //Act
        await _productService.DeleteSimilarProduct(similarProduct);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.SimilarProducts.Count);
    }

    [TestMethod]
    public async Task InsertBundleProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        //Act
        await _productService.InsertBundleProduct(new BundleProduct {
            ProductId = "2"
        }, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.BundleProducts.Count);
    }

    [TestMethod]
    public async Task UpdateBundleProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var bundleProduct = new BundleProduct { ProductId = "2" };
        await _productService.InsertBundleProduct(bundleProduct, product.Id);

        //Act
        bundleProduct.Quantity = 10;
        await _productService.UpdateBundleProduct(bundleProduct, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.BundleProducts.Count);
        Assert.AreEqual(10, result.BundleProducts.FirstOrDefault().Quantity);
    }

    [TestMethod]
    public async Task DeleteBundleProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var bundleProduct = new BundleProduct { ProductId = "2" };
        await _productService.InsertBundleProduct(bundleProduct, product.Id);

        //Act
        await _productService.DeleteBundleProduct(bundleProduct, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.BundleProducts.Count);
    }

    [TestMethod]
    public async Task InsertCrossSellProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);

        //Act
        await _productService.InsertCrossSellProduct(new CrossSellProduct {
            ProductId1 = product.Id,
            ProductId2 = "2"
        });

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.CrossSellProduct.Count);
    }

    [TestMethod]
    public async Task DeleteCrossSellProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var crossSellProduct = new CrossSellProduct {
            ProductId1 = product.Id,
            ProductId2 = "2"
        };
        await _productService.InsertCrossSellProduct(crossSellProduct);

        //Act
        await _productService.DeleteCrossSellProduct(crossSellProduct);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.CrossSellProduct.Count);
    }

    [TestMethod]
    public async Task GetCrossSellProductsByShoppingCartTest()
    {
        //Arrange 
        var product = new Product { Published = true, VisibleIndividually = true };
        await _productService.InsertProduct(product);
        var product2 = new Product { Published = true, VisibleIndividually = true };
        await _productService.InsertProduct(product2);
        var product3 = new Product { Published = true, VisibleIndividually = true };
        await _productService.InsertProduct(product3);
        //Act
        await _productService.InsertCrossSellProduct(new CrossSellProduct {
            ProductId1 = product.Id,
            ProductId2 = product2.Id
        });
        //Act
        await _productService.InsertCrossSellProduct(new CrossSellProduct {
            ProductId1 = product.Id,
            ProductId2 = product3.Id
        });

        var result =
            await _productService.GetCrossSellProductsByShoppingCart(
                new List<ShoppingCartItem> { new() { ProductId = product.Id } }, 2);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task InsertRecommendedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);

        //Act
        await _productService.InsertRecommendedProduct(product.Id, "2");

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.RecommendedProduct.Count);
    }

    [TestMethod]
    public async Task DeleteRecommendedProductTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        await _productService.InsertRecommendedProduct(product.Id, "2");

        //Act
        await _productService.DeleteRecommendedProduct(product.Id, "2");

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.AreEqual(0, result.RecommendedProduct.Count);
    }

    [TestMethod]
    public async Task InsertTierPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);

        //Act
        await _productService.InsertTierPrice(new TierPrice { Price = 10, Quantity = 1 }, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.TierPrices.Count);
    }

    [TestMethod]
    public async Task UpdateTierPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var tierprice = new TierPrice { Price = 10, Quantity = 1 };
        await _productService.InsertTierPrice(tierprice, product.Id);

        //Act
        tierprice.Quantity = 10;
        await _productService.UpdateTierPrice(tierprice, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.TierPrices.Count);
        Assert.AreEqual(10, result.TierPrices.FirstOrDefault().Quantity);
    }

    [TestMethod]
    public async Task DeleteTierPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var tierprice = new TierPrice { Price = 10, Quantity = 1 };
        await _productService.InsertTierPrice(tierprice, product.Id);

        //Act
        await _productService.DeleteTierPrice(tierprice, product.Id);
        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TierPrices.Count);
    }

    [TestMethod]
    public async Task InsertProductPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPrice = new ProductPrice { Price = 10, CurrencyCode = "USD", ProductId = product.Id };

        //Act
        await _productService.InsertProductPrice(productPrice);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductPrices.Count);
    }

    [TestMethod]
    public async Task UpdateProductPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPrice = new ProductPrice { Price = 10, CurrencyCode = "USD", ProductId = product.Id };
        await _productService.InsertProductPrice(productPrice);

        //Act
        productPrice.CurrencyCode = "EUR";
        await _productService.UpdateProductPrice(productPrice);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductPrices.Count);
        Assert.AreEqual("EUR", result.ProductPrices.FirstOrDefault().CurrencyCode);
    }

    [TestMethod]
    public async Task DeleteProductPriceTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPrice = new ProductPrice { Price = 10, CurrencyCode = "USD", ProductId = product.Id };
        await _productService.InsertProductPrice(productPrice);

        //Act
        await _productService.DeleteProductPrice(productPrice);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.ProductPrices.Count);
    }

    [TestMethod]
    public async Task InsertProductPictureTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPicture = new ProductPicture { PictureId = "1" };

        //Act
        await _productService.InsertProductPicture(productPicture, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductPictures.Count);
    }

    [TestMethod]
    public async Task UpdateProductPictureTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPicture = new ProductPicture { PictureId = "1" };
        await _productService.InsertProductPicture(productPicture, product.Id);

        //Act
        productPicture.PictureId = "2";
        await _productService.UpdateProductPicture(productPicture, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductPictures.Count);
        Assert.AreEqual("2", result.ProductPictures.FirstOrDefault().PictureId);
    }

    [TestMethod]
    public async Task DeleteProductPictureTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productPicture = new ProductPicture { PictureId = "1" };
        await _productService.InsertProductPicture(productPicture, product.Id);

        //Act
        await _productService.DeleteProductPicture(productPicture, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.ProductPictures.Count);
    }

    [TestMethod]
    public async Task InsertProductWarehouseInventoryTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productWarehouseInventory = new ProductWarehouseInventory { WarehouseId = "1" };

        //Act
        await _productService.InsertProductWarehouseInventory(productWarehouseInventory, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductWarehouseInventory.Count);
    }

    [TestMethod]
    public async Task UpdateProductWarehouseInventoryTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productWarehouseInventory = new ProductWarehouseInventory { WarehouseId = "1" };
        await _productService.InsertProductWarehouseInventory(productWarehouseInventory, product.Id);

        //Act
        productWarehouseInventory.WarehouseId = "2";
        await _productService.UpdateProductWarehouseInventory(productWarehouseInventory, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.ProductWarehouseInventory.Count);
        Assert.AreEqual("2", result.ProductWarehouseInventory.FirstOrDefault().WarehouseId);
    }

    [TestMethod]
    public async Task DeleteProductWarehouseInventoryTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        var productWarehouseInventory = new ProductWarehouseInventory { WarehouseId = "1" };
        await _productService.InsertProductWarehouseInventory(productWarehouseInventory, product.Id);

        //Act
        await _productService.DeleteProductWarehouseInventory(productWarehouseInventory, product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.ProductWarehouseInventory.Count);
    }

    [TestMethod]
    public async Task DeleteDiscountTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);
        await _productService.InsertDiscount("1", product.Id);

        //Act
        await _productService.DeleteDiscount("1", product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.AppliedDiscounts.Count);
    }

    [TestMethod]
    public async Task InsertDiscountTest()
    {
        //Arrange 
        var product = new Product();
        await _productService.InsertProduct(product);

        //Act
        await _productService.InsertDiscount("1", product.Id);

        var result = await _productService.GetProductById(product.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.AppliedDiscounts.Count);
    }
}