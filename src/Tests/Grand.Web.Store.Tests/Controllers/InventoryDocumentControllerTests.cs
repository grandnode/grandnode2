using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class InventoryDocumentControllerTests
{
    private const string StoreId = "store1";

    private InventoryDocumentController _controller;
    private Mock<IInventoryDocumentService> _documentService;
    private Mock<IAdminDataScope<MerchandiseReturn>> _merchandiseReturnScope;
    private readonly Mock<IMerchandiseReturnService> _merchandiseReturnService = new();
    private Mock<IAdminDataScope<Product>> _productScope;
    private Mock<IProductService> _productService;
    private InventoryDocumentViewModelService _viewModelService;

    [TestInitialize]
    public void Setup()
    {
        var workContext = new Mock<IWorkContext>();
        workContext.Setup(x => x.CurrentCustomer).Returns(new Customer { Id = "staff", StaffStoreId = StoreId });
        var contextAccessor = new Mock<IContextAccessor>();
        contextAccessor.Setup(x => x.WorkContext).Returns(workContext.Object);

        //w-own of this store, w-other of another store, w-shared of none
        var warehouses = new List<Warehouse> {
            new() { Id = "w-own", StoreId = StoreId }, new() { Id = "w-other", StoreId = "store2" },
            new() { Id = "w-shared", StoreId = "" }
        };
        var warehouseService = new Mock<IWarehouseService>();
        foreach (var warehouse in warehouses)
            warehouseService.Setup(x => x.GetWarehouseById(warehouse.Id)).ReturnsAsync(warehouse);
        warehouseService.Setup(x => x.GetAllWarehouses(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<Warehouse>(warehouses, 0, 10));
        var warehouseScope = new StoreWarehouseDataScope(contextAccessor.Object);

        _documentService = new Mock<IInventoryDocumentService>();
        _documentService.Setup(x => x.UpdateDraft(It.IsAny<InventoryDocument>())).ReturnsAsync([]);
        _documentService.Setup(x => x.Search(It.IsAny<InventoryDocumentSearch>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedList<InventoryDocument>());
        _productService = new Mock<IProductService>();
        _productScope = new Mock<IAdminDataScope<Product>>();
        _merchandiseReturnScope = new Mock<IAdminDataScope<MerchandiseReturn>>();
        var translationService = new Mock<IRequestTranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>())).Returns("resource");

        _viewModelService = new InventoryDocumentViewModelService(_documentService.Object, warehouseService.Object,
            _productService.Object, new Mock<ICategoryService>().Object, new Mock<IBrandService>().Object,
            translationService.Object, new Mock<IEnumTranslationService>().Object, new Mock<IDateTimeService>().Object,
            warehouseScope, _productScope.Object);
        _controller = new InventoryDocumentController(_documentService.Object, _viewModelService,
            warehouseService.Object, _merchandiseReturnService.Object, translationService.Object,
            contextAccessor.Object, warehouseScope, _merchandiseReturnScope.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private InventoryDocument Stored(string storeId, string warehouseId,
        InventoryDocumentStatus status = InventoryDocumentStatus.Draft)
    {
        var document = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.Receipt, StoreId = storeId, WarehouseId = warehouseId,
            StatusId = status
        };
        _documentService.Setup(x => x.GetById(document.Id)).ReturnsAsync(document);
        return document;
    }

    private static void AssertRedirectedTo(IActionResult result, string action)
    {
        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.AreEqual(action, ((RedirectToActionResult)result).ActionName);
    }

    [TestMethod]
    public async Task Store_Edit_OtherStoreWarehouse_AccessDenied()
    {
        var document = Stored("store2", "w-other");

        AssertRedirectedTo(await _controller.Edit(document.Id), "List");
    }

    [TestMethod]
    public async Task Store_Edit_OwnWarehouse_Shown()
    {
        var document = Stored(StoreId, "w-own");

        Assert.IsInstanceOfType<ViewResult>(await _controller.Edit(document.Id));
    }

    [TestMethod]
    public async Task Store_CreateAssignment_OtherStoreWarehouse_Rejected()
    {
        var result = await _controller.Create(new InventoryDocumentModel
            { DocumentTypeId = (int)InventoryDocumentType.Assignment, WarehouseId = "w-other" });

        AssertRedirectedTo(result, "List");
        _documentService.Verify(x => x.CreateDraft(It.IsAny<InventoryDocument>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_Create_SharedWarehouse_Rejected()
    {
        var result = await _controller.Create(new InventoryDocumentModel
            { DocumentTypeId = (int)InventoryDocumentType.Receipt, WarehouseId = "w-shared" });

        AssertRedirectedTo(result, "List");
        _documentService.Verify(x => x.CreateDraft(It.IsAny<InventoryDocument>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_SaveDraft_TamperedWarehouseToShared_Rejected()
    {
        var document = Stored(StoreId, "w-own");

        var result = await _controller.Edit(new InventoryDocumentModel { Id = document.Id, WarehouseId = "w-shared" },
            false);

        AssertRedirectedTo(result, "List");
        _documentService.Verify(x => x.UpdateDraft(It.IsAny<InventoryDocument>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_AddLine_OtherStoreProduct_Rejected()
    {
        var document = Stored(StoreId, "w-own");
        var product = new Product { Sku = "SKU", ManageInventoryMethodId = ManageInventoryMethod.ManageStock };
        _productService.Setup(x => x.GetProductBySku("SKU")).ReturnsAsync(product);
        _productScope.Setup(x => x.HasAccess(product)).ReturnsAsync(false);

        await _controller.Edit(new InventoryDocumentModel { Id = document.Id, WarehouseId = "w-own", AddSku = "SKU" },
            true);

        _documentService.Verify(x => x.UpdateDraft(It.IsAny<InventoryDocument>()), Times.Never);
        Assert.AreEqual(0, document.Lines.Count);
    }

    [TestMethod]
    public async Task Store_AddLine_OwnProduct_Saved()
    {
        var document = Stored(StoreId, "w-own");
        var product = new Product { Sku = "SKU", ManageInventoryMethodId = ManageInventoryMethod.ManageStock };
        _productService.Setup(x => x.GetProductBySku("SKU")).ReturnsAsync(product);
        _productScope.Setup(x => x.HasAccess(product)).ReturnsAsync(true);

        await _controller.Edit(new InventoryDocumentModel { Id = document.Id, WarehouseId = "w-own", AddSku = "SKU" },
            true);

        _documentService.Verify(x => x.UpdateDraft(It.Is<InventoryDocument>(d =>
            d.Lines.Single().ProductId == product.Id)), Times.Once);
    }

    [TestMethod]
    public async Task AddLineToStockCount_TakesTheStoredStockAsExpected()
    {
        var document = Stored(StoreId, "w-own");
        document.DocumentTypeId = InventoryDocumentType.StockCount;
        var product = new Product
            { Sku = "SKU", ManageInventoryMethodId = ManageInventoryMethod.ManageStock, StockQuantity = 7 };
        _productService.Setup(x => x.GetProductBySku("SKU")).ReturnsAsync(product);
        _productScope.Setup(x => x.HasAccess(product)).ReturnsAsync(true);

        await _controller.Edit(new InventoryDocumentModel { Id = document.Id, WarehouseId = "w-own", AddSku = "SKU" },
            true);

        Assert.AreEqual(7, document.Lines.Single().ExpectedQuantity);
    }

    [TestMethod]
    public async Task Store_Post_OtherStoreDocument_AccessDenied()
    {
        var document = Stored("store2", "w-other");

        AssertRedirectedTo(await _controller.Post(document.Id), "List");
        _documentService.Verify(x => x.Post(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_Post_DocumentWithOtherStoreProduct_AccessDenied()
    {
        var document = Stored(StoreId, "w-own");
        var product = new Product();
        document.Lines.Add(new InventoryDocumentLine { ProductId = product.Id, Quantity = 1 });
        _productService.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
        _productScope.Setup(x => x.HasAccess(product)).ReturnsAsync(false);

        AssertRedirectedTo(await _controller.Post(document.Id), "List");
        _documentService.Verify(x => x.Post(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Store_CreateStockCount_LeavesOutProductsOfOtherStores()
    {
        var own = new Product();
        var foreign = new Product();
        foreach (var product in new[] { own, foreign })
            _productService.Setup(x => x.GetProductById(product.Id, false)).ReturnsAsync(product);
        _productScope.Setup(x => x.HasAccess(own)).ReturnsAsync(true);
        _productScope.Setup(x => x.HasAccess(foreign)).ReturnsAsync(false);
        var count = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.StockCount, StoreId = StoreId, WarehouseId = "w-own",
            Lines = { new InventoryDocumentLine { ProductId = own.Id }, new InventoryDocumentLine { ProductId = foreign.Id } }
        };
        _documentService.Setup(x => x.CreateStockCount("w-own", It.IsAny<IList<string>>(),
            It.IsAny<IList<string>>(), "staff")).ReturnsAsync((count, []));

        await _controller.Create(new InventoryDocumentModel
            { DocumentTypeId = (int)InventoryDocumentType.StockCount, WarehouseId = "w-own" });

        _documentService.Verify(x => x.UpdateDraft(It.Is<InventoryDocument>(d =>
            d.Lines.Single().ProductId == own.Id)), Times.Once);
    }

    [TestMethod]
    public async Task Store_CreateReturnReceipt_OnlyProductsOfOtherStores_DraftDeleted()
    {
        var foreign = new Product();
        _productService.Setup(x => x.GetProductById(foreign.Id, false)).ReturnsAsync(foreign);
        _productScope.Setup(x => x.HasAccess(foreign)).ReturnsAsync(false);
        _merchandiseReturnScope.Setup(x => x.HasAccess(It.IsAny<MerchandiseReturn>())).ReturnsAsync(true);
        _merchandiseReturnService.Setup(x => x.GetMerchandiseReturnById("return"))
            .ReturnsAsync(new MerchandiseReturn());
        var receipt = new InventoryDocument {
            DocumentTypeId = InventoryDocumentType.ReturnReceipt, StoreId = StoreId, WarehouseId = "w-own",
            Lines = { new InventoryDocumentLine { ProductId = foreign.Id, Quantity = 1 } }
        };
        _documentService.Setup(x => x.CreateReturnReceipt("return", "staff")).ReturnsAsync((receipt, []));

        AssertRedirectedTo(await _controller.CreateReturnReceipt("return"), "List");
        _documentService.Verify(x => x.DeleteDraft(receipt.Id), Times.Once);
    }

    [TestMethod]
    public async Task Store_List_FiltersByStaffStore()
    {
        await _viewModelService.PrepareRows(new InventoryDocumentListModel(), 0, 10);

        _documentService.Verify(x => x.Search(It.Is<InventoryDocumentSearch>(s => s.StoreId == StoreId),
            0, 10), Times.Once);
    }

    [TestMethod]
    public async Task Store_UsableWarehouses_OwnOnly()
    {
        var usable = await _viewModelService.UsableWarehouses();

        CollectionAssert.AreEqual(new[] { "w-own" }, usable.Select(x => x.Id).ToArray());
    }

    [TestMethod]
    public async Task Store_CreateReturnReceipt_OtherStoreReturn_AccessDenied()
    {
        _merchandiseReturnScope.Setup(x => x.HasAccess(It.IsAny<MerchandiseReturn>())).ReturnsAsync(false);

        AssertRedirectedTo(await _controller.CreateReturnReceipt("return"), "List");
        _documentService.Verify(x => x.CreateReturnReceipt(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_Posted_Rejected()
    {
        var document = Stored(StoreId, "w-own", InventoryDocumentStatus.Posted);

        var result = await _controller.Edit(new InventoryDocumentModel { Id = document.Id, WarehouseId = "w-own" },
            false);

        AssertRedirectedTo(result, "Edit");
        _documentService.Verify(x => x.UpdateDraft(It.IsAny<InventoryDocument>()), Times.Never);
    }

    [TestMethod]
    public async Task Delete_Posted_Rejected()
    {
        var document = Stored(StoreId, "w-own", InventoryDocumentStatus.Posted);
        _documentService.Setup(x => x.DeleteDraft(document.Id)).ReturnsAsync([InventoryDocumentErrors.Posted]);

        AssertRedirectedTo(await _controller.Delete(document.Id), "Edit");
    }

    [TestMethod]
    public void Controllers_HavePermissionAttributes()
    {
        var type = typeof(InventoryDocumentController);
        var permission = type.GetCustomAttributes(typeof(PermissionAuthorizeAttribute), true)
            .Cast<PermissionAuthorizeAttribute>().Single();
        Assert.AreEqual(PermissionSystemName.InventoryDocuments, permission.Permission);
        foreach (var action in type.GetMethods().Where(x => x.DeclaringType == type))
            Assert.IsTrue(action.GetCustomAttributes(typeof(PermissionAuthorizeActionAttribute), true).Length > 0,
                action.Name);
    }
}
