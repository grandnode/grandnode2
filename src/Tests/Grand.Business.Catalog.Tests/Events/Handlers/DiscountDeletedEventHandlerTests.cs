using Grand.Business.Catalog.Events.Handlers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Events;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class DiscountDeletedEventHandlerTests
{
    private IRepository<Brand> _brandRepository;
    private MemoryCacheBase _cacheBase;
    private IRepository<Category> _categoryRepository;
    private IRepository<Collection> _collectionRepository;
    private IRepository<DiscountCoupon> _discountCouponRepository;

    private DiscountDeletedEventHandler _handler;
    private Mock<IMediator> _mediatorMock;
    private IRepository<Product> _productRepository;
    private IRepository<Vendor> _vendorRepository;

    [TestInitialize]
    public void Init()
    {
        _mediatorMock = new Mock<IMediator>();
        _productRepository = new MongoDBRepositoryTest<Product>();
        _categoryRepository = new MongoDBRepositoryTest<Category>();
        _brandRepository = new MongoDBRepositoryTest<Brand>();
        _collectionRepository = new MongoDBRepositoryTest<Collection>();
        _vendorRepository = new MongoDBRepositoryTest<Vendor>();
        _discountCouponRepository = new MongoDBRepositoryTest<DiscountCoupon>();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });

        _handler = new DiscountDeletedEventHandler(_productRepository, _categoryRepository,
            _brandRepository, _collectionRepository, _vendorRepository, _discountCouponRepository, _cacheBase);
    }

    [TestMethod]
    public async Task Handle_Product_Test()
    {
        //Arrange
        var discount = new Discount { DiscountTypeId = DiscountType.AssignedToSkus };
        var product = new Product();
        product.AppliedDiscounts.Add(discount.Id);
        await _productRepository.InsertAsync(product);
        var product3 = new Product();
        product3.AppliedDiscounts.Add("1");
        await _productRepository.InsertAsync(product3);

        //Act
        await _handler.Handle(new EntityDeleted<Discount>(discount), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _productRepository.Table.Where(x => x.AppliedDiscounts.Contains(discount.Id)).Count());
        Assert.AreEqual(1, _productRepository.Table.Where(x => x.AppliedDiscounts.Contains("1")).Count());
    }

    [TestMethod]
    public async Task Handle_Category_Test()
    {
        //Arrange
        var discount = new Discount { DiscountTypeId = DiscountType.AssignedToCategories };
        var category = new Category();
        category.AppliedDiscounts.Add(discount.Id);
        await _categoryRepository.InsertAsync(category);
        var category3 = new Category();
        category3.AppliedDiscounts.Add("1");
        await _categoryRepository.InsertAsync(category3);

        //Act
        await _handler.Handle(new EntityDeleted<Discount>(discount), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _categoryRepository.Table.Where(x => x.AppliedDiscounts.Contains(discount.Id)).Count());
        Assert.AreEqual(1, _categoryRepository.Table.Where(x => x.AppliedDiscounts.Contains("1")).Count());
    }

    [TestMethod]
    public async Task Handle_Collection_Test()
    {
        //Arrange
        var discount = new Discount { DiscountTypeId = DiscountType.AssignedToCollections };
        var collection = new Collection();
        collection.AppliedDiscounts.Add(discount.Id);
        await _collectionRepository.InsertAsync(collection);
        var collection3 = new Collection();
        collection3.AppliedDiscounts.Add("1");
        await _collectionRepository.InsertAsync(collection3);

        //Act
        await _handler.Handle(new EntityDeleted<Discount>(discount), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _collectionRepository.Table.Where(x => x.AppliedDiscounts.Contains(discount.Id)).Count());
        Assert.AreEqual(1, _collectionRepository.Table.Where(x => x.AppliedDiscounts.Contains("1")).Count());
    }

    [TestMethod]
    public async Task Handle_Vendor_Test()
    {
        //Arrange
        var discount = new Discount { DiscountTypeId = DiscountType.AssignedToVendors };
        var vendor = new Vendor();
        vendor.AppliedDiscounts.Add(discount.Id);
        await _vendorRepository.InsertAsync(vendor);
        var vendor3 = new Vendor();
        vendor3.AppliedDiscounts.Add("1");
        await _vendorRepository.InsertAsync(vendor3);

        //Act
        await _handler.Handle(new EntityDeleted<Discount>(discount), CancellationToken.None);
        //Assert
        Assert.AreEqual(0, _vendorRepository.Table.Where(x => x.AppliedDiscounts.Contains(discount.Id)).Count());
        Assert.AreEqual(1, _vendorRepository.Table.Where(x => x.AppliedDiscounts.Contains("1")).Count());
    }
}