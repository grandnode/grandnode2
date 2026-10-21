using Grand.Business.Catalog.Services.Products;
using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
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

    [TestInitialize]
    public void InitializeTests()
    {
        _productRepository = new MongoDBRepositoryTest<Product>();
        _mediatorMock = new Mock<IMediator>();

        _mediatorMock.Setup(x => x.Send(It.IsAny<GetProductArchByIdQuery>(), default))
            .Returns(Task.FromResult(new Product()));

        _aclServiceMock = new AclService(new AccessControlConfig());
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _productService = new ProductService(_cacheBase, _productRepository,
            _mediatorMock.Object, _aclServiceMock);
    }

    [TestMethod]
    public async Task GetProductsByIds_AuthorizesAgainstGivenCustomerAndStore()
    {
        //Arrange
        await _productRepository.InsertAsync(new Product {
            Id = "p1", Published = true, LimitedToStores = true, Stores = new List<string> { "store1" }
        });

        //Act
        var store1 = await _productService.GetProductsByIds(["p1"], CatalogAccess.For(new Customer(), "store1"));
        var store2 = await _productService.GetProductsByIds(["p1"], CatalogAccess.For(new Customer(), "store2"));

        //Assert
        Assert.HasCount(1, store1);
        Assert.IsEmpty(store2);
    }

    [TestMethod]
    public async Task GetProductsByIds_WithoutAccess_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _productService.GetProductsByIds(["p1"], null));
    }

    [TestMethod]
    public async Task GetProductsByIds_IncludingHidden_ReturnsUnpublishedAndGroupLimitedProducts()
    {
        //Arrange
        await _productRepository.InsertAsync(new Product {
            Id = "p1", Published = false, LimitedToGroups = true, CustomerGroups = new List<string> { "vip" }
        });

        //Act
        var products = await _productService.GetProductsByIds(["p1"], CatalogAccess.IncludingHidden());

        //Assert
        Assert.HasCount(1, products);
    }

    [TestMethod]
    public async Task GetProductsByIds_ColdCache_LoadsMissesWithOneQueryAndFillsCache()
    {
        //Arrange
        var stored = new List<Product> {
            new() { Id = "p1", Published = true }, new() { Id = "p2", Published = true },
            new() { Id = "p3", Published = true }
        };
        var repositoryMock = new Mock<IRepository<Product>>();
        repositoryMock.Setup(x => x.Table).Returns(stored.AsQueryable());
        repositoryMock.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Product> query, CancellationToken _) => query.ToList());
        var productService = new ProductService(_cacheBase, repositoryMock.Object, _mediatorMock.Object,
            _aclServiceMock);
        var access = CatalogAccess.For(new Customer(), "");

        //Act
        var first = await productService.GetProductsByIds(["p1", "p2", "p3"], access);
        var second = await productService.GetProductsByIds(["p3", "p1"], access);

        //Assert
        Assert.HasCount(3, first);
        CollectionAssert.AreEqual(new[] { "p3", "p1" }, second.Select(x => x.Id).ToArray());
        repositoryMock.Verify(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()),
            Times.Once);
        repositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task GetProductsByIds_MissAfterHits_QueriesOnlyTheMisses()
    {
        //Arrange
        var stored = new List<Product> {
            new() { Id = "p1", Published = true }, new() { Id = "p2", Published = true },
            new() { Id = "p3", Published = true }
        };
        var queried = new List<string>();
        var repositoryMock = new Mock<IRepository<Product>>();
        repositoryMock.Setup(x => x.Table).Returns(stored.AsQueryable());
        repositoryMock.Setup(x => x.ToListAsync(It.IsAny<IQueryable<Product>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IQueryable<Product> query, CancellationToken _) => {
                var result = query.ToList();
                queried.AddRange(result.Select(x => x.Id));
                return result;
            });
        var productService = new ProductService(_cacheBase, repositoryMock.Object, _mediatorMock.Object,
            _aclServiceMock);
        var access = CatalogAccess.For(new Customer(), "");
        await productService.GetProductsByIds(["p1", "p3"], access);
        queried.Clear();

        //Act
        var products = await productService.GetProductsByIds(["p1", "p2", "p3", "p2"], access);

        //Assert
        CollectionAssert.AreEqual(new[] { "p1", "p2", "p3", "p2" }, products.Select(x => x.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "p2" }, queried);
    }

    [TestMethod]
    public async Task GetProductsByIds_PartlyCached_KeepsOrderDuplicatesAndSkipsMissing()
    {
        //Arrange
        await _productRepository.InsertAsync(new Product { Id = "p1", Published = true });
        await _productRepository.InsertAsync(new Product { Id = "p2", Published = true });
        await _productService.GetProductById("p2");

        //Act
        var products = await _productService.GetProductsByIds(["p2", "", "x", "p1", "p2", null],
            CatalogAccess.For(new Customer(), ""));

        //Assert
        CollectionAssert.AreEqual(new[] { "p2", "p1", "p2" }, products.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task SearchProducts_SendsGivenCustomerInQuery()
    {
        //Arrange
        var customer = new Customer { Id = "c1" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSearchProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new PagedList<Product>(new List<Product>(), 0, 1), (IList<string>)new List<string>()));

        //Act
        await _productService.SearchProducts(CatalogAccess.For(customer, "s1"));

        //Assert
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetSearchProductsQuery>(q => q.Customer == customer && q.StoreId == "s1"),
            It.IsAny<CancellationToken>()), Times.Once);
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
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetAllProductsDisplayedOnBestSellerTest()
    {
        //Arrange 
        await insertSampleProducts();

        //Act
        var result = await _productService.GetAllProductsDisplayedOnBestSeller();

        //Assert
        Assert.HasCount(2, result);
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
        var result = await _productService.GetProductsByIds(["1"], CatalogAccess.For(new Customer(), ""));

        //Assert
        Assert.IsNotEmpty(result);
    }

    [TestMethod]
    public async Task GetProductsByDiscountTest()
    {
        //Arrange
        await insertSampleProducts();

        //Act
        var result = await _productService.GetProductsByDiscount("1");

        //Assert
        Assert.IsNotEmpty(result);
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
        Assert.AreEqual("test2", result.Name);
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
        var result = _productService.GetCategoryProductNumber(new Customer(), ["1"]);

        //Assert
        Assert.AreEqual(1, result);
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
        Assert.IsNotEmpty(result);
    }

    [TestMethod]
    public async Task GetAssociatedProductsTest()
    {
        //Arrange 
        await insertSampleProducts();
        //Act
        var result = await _productService.GetAssociatedProducts("1", CatalogAccess.For(new Customer(), ""));

        //Assert
        Assert.IsNotEmpty(result);
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
        Assert.HasCount(1, result.RelatedProducts);
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
        Assert.IsEmpty(result.RelatedProducts);
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
        Assert.HasCount(1, result.RelatedProducts);
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
        Assert.HasCount(1, result.SimilarProducts);
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
        Assert.HasCount(1, result.SimilarProducts);
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
        Assert.IsEmpty(result.SimilarProducts);
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
        Assert.HasCount(1, result.BundleProducts);
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
        Assert.HasCount(1, result.BundleProducts);
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
        Assert.IsEmpty(result.BundleProducts);
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
        Assert.HasCount(1, result.CrossSellProduct);
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
        Assert.IsEmpty(result.CrossSellProduct);
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
                new List<ShoppingCartItem> { new() { ProductId = product.Id } }, 2, new Customer(), "");

        //Assert
        Assert.IsNotNull(result);
        Assert.HasCount(2, result);
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
        Assert.HasCount(1, result.RecommendedProduct);
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
        Assert.IsEmpty(result.RecommendedProduct);
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
        Assert.HasCount(1, result.TierPrices);
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
        Assert.HasCount(1, result.TierPrices);
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
        Assert.IsEmpty(result.TierPrices);
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
        Assert.HasCount(1, result.ProductPrices);
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
        Assert.HasCount(1, result.ProductPrices);
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
        Assert.IsEmpty(result.ProductPrices);
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
        Assert.HasCount(1, result.ProductPictures);
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
        Assert.HasCount(1, result.ProductPictures);
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
        Assert.IsEmpty(result.ProductPictures);
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
        Assert.HasCount(1, result.ProductWarehouseInventory);
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
        Assert.HasCount(1, result.ProductWarehouseInventory);
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
        Assert.IsEmpty(result.ProductWarehouseInventory);
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
        Assert.IsEmpty(result.AppliedDiscounts);
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
        Assert.HasCount(1, result.AppliedDiscounts);
    }
}