using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Inventory;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Services;

public interface IInventoryDocumentViewModelService
{
    Task<InventoryDocumentListModel> PrepareListModel();

    Task<(IList<InventoryDocumentRowModel> Rows, int TotalCount)> PrepareRows(InventoryDocumentListModel model,
        int pageIndex, int pageSize);

    /// <summary>
    ///     The model of a document; with a null document, of a new one of the model's type
    /// </summary>
    Task PrepareModel(InventoryDocumentModel model, InventoryDocument document);

    /// <summary>
    ///     The warehouses the current user may put on a document
    /// </summary>
    Task<IList<Warehouse>> UsableWarehouses();

    /// <summary>
    ///     Writes the posted header and lines onto the draft, a product added by SKU included
    /// </summary>
    /// <returns>The translated errors; the draft is not to be saved when there is one</returns>
    Task<IList<string>> ApplyModel(InventoryDocument document, InventoryDocumentModel model);

    /// <summary>
    ///     The translated messages of the keys a service returned
    /// </summary>
    IList<string> Translate(IEnumerable<string> keys);

    /// <summary>
    ///     The lines whose product fails the panel's product access check
    /// </summary>
    Task<IList<InventoryDocumentLine>> InaccessibleLines(InventoryDocument document);
}

public class InventoryDocumentViewModelService(
    IInventoryDocumentService inventoryDocumentService,
    IWarehouseService warehouseService,
    IProductService productService,
    ICategoryService categoryService,
    IBrandService brandService,
    IRequestTranslationService translationService,
    IEnumTranslationService enumTranslationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Warehouse> warehouseScope,
    IAdminDataScope<Product> productScope) : IInventoryDocumentViewModelService
{
    public async Task<InventoryDocumentListModel> PrepareListModel()
    {
        var model = new InventoryDocumentListModel();
        model.AvailableDocumentTypes.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var type in Enum.GetValues<InventoryDocumentType>())
            model.AvailableDocumentTypes.Add(new SelectListItem
                { Text = enumTranslationService.GetTranslationEnum(type), Value = ((int)type).ToString() });
        model.AvailableStatuses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var status in Enum.GetValues<InventoryDocumentStatus>())
            model.AvailableStatuses.Add(new SelectListItem
                { Text = enumTranslationService.GetTranslationEnum(status), Value = ((int)status).ToString() });
        model.AvailableWarehouses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await UsableWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });
        return model;
    }

    public async Task<(IList<InventoryDocumentRowModel> Rows, int TotalCount)> PrepareRows(
        InventoryDocumentListModel model, int pageIndex, int pageSize)
    {
        var documents = await inventoryDocumentService.Search(new InventoryDocumentSearch {
            Type = (InventoryDocumentType?)model.SearchDocumentTypeId,
            Status = (InventoryDocumentStatus?)model.SearchStatusId,
            WarehouseId = model.SearchWarehouseId,
            Number = model.SearchNumber,
            //Store sees the documents of its own warehouses only
            StoreId = await warehouseScope.GetDefaultStoreIdAsync()
        }, pageIndex, pageSize);

        var warehouses = (await warehouseService.GetAllWarehouses()).ToDictionary(x => x.Id, x => x.Name);
        var rows = documents.Select(x => new InventoryDocumentRowModel {
            Id = x.Id,
            Number = x.Number,
            DocumentType = enumTranslationService.GetTranslationEnum(x.DocumentTypeId),
            Status = enumTranslationService.GetTranslationEnum(x.StatusId),
            Warehouse = warehouses.GetValueOrDefault(x.WarehouseId ?? "") ?? "",
            Lines = x.Lines.Count,
            CreatedOn = dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
        }).ToList();
        return (rows, documents.TotalCount);
    }

    public async Task<IList<Warehouse>> UsableWarehouses()
    {
        var usable = new List<Warehouse>();
        foreach (var warehouse in await warehouseService.GetAllWarehouses())
            if (await warehouseScope.HasAccess(warehouse))
                usable.Add(warehouse);
        return usable;
    }

    public async Task PrepareModel(InventoryDocumentModel model, InventoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (document != null)
        {
            model.Id = document.Id;
            model.Number = document.Number;
            model.DocumentTypeId = (int)document.DocumentTypeId;
            model.Posted = document.StatusId == InventoryDocumentStatus.Posted;
            model.Status = enumTranslationService.GetTranslationEnum(document.StatusId);
            model.WarehouseId = document.WarehouseId;
            model.TargetWarehouseId = document.TargetWarehouseId;
            model.SupplierName = document.SupplierName;
            model.ExternalReference = document.ExternalReference;
            model.Comment = document.Comment;
            model.Lines.Clear();
            foreach (var line in document.Lines)
            {
                var product = await productService.GetProductById(line.ProductId);
                model.Lines.Add(new InventoryDocumentLineModel {
                    Id = line.Id,
                    ProductName = product?.Name ?? line.ProductId,
                    Sku = product?.FindProductAttributeCombination(line.Attributes)?.Sku ?? product?.Sku,
                    AttributesText = product == null
                        ? ""
                        : string.Join(", ", product.ParseProductAttributeValues(line.Attributes).Select(x => x.Name)),
                    Quantity = line.Quantity,
                    ExpectedQuantity = line.ExpectedQuantity,
                    CountedQuantity = line.CountedQuantity,
                    UnitCost = line.UnitCost,
                    ConditionId = (int)line.Condition,
                    Comment = line.Comment
                });
            }
        }

        model.DocumentType = enumTranslationService.GetTranslationEnum((InventoryDocumentType)model.DocumentTypeId);
        model.AvailableWarehouses.Clear();
        model.AvailableWarehouses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await UsableWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });

        if (document == null && model.DocumentTypeId == (int)InventoryDocumentType.StockCount)
        {
            var storeId = await productScope.GetDefaultStoreIdAsync() ?? "";
            foreach (var category in await categoryService.GetAllCategories("", "",
                         CatalogAccess.IncludingHidden(storeId)))
                model.AvailableCategories.Add(new SelectListItem { Text = category.Name, Value = category.Id });
            foreach (var brand in await brandService.GetAllBrands("", CatalogAccess.IncludingHidden(storeId)))
                model.AvailableBrands.Add(new SelectListItem { Text = brand.Name, Value = brand.Id });
        }
    }

    public async Task<IList<string>> ApplyModel(InventoryDocument document, InventoryDocumentModel model)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(model);

        var errors = new List<string>();
        document.WarehouseId = model.WarehouseId;
        document.TargetWarehouseId = document.DocumentTypeId == InventoryDocumentType.Transfer
            ? model.TargetWarehouseId
            : null;
        document.SupplierName = model.SupplierName;
        document.ExternalReference = model.ExternalReference;
        document.Comment = model.Comment;

        foreach (var lineModel in model.Lines)
        {
            var line = document.Lines.FirstOrDefault(x => x.Id == lineModel.Id);
            if (line == null)
                continue;
            if (lineModel.Remove)
            {
                document.Lines.Remove(line);
                continue;
            }

            line.Quantity = lineModel.Quantity;
            line.CountedQuantity = lineModel.CountedQuantity;
            line.UnitCost = lineModel.UnitCost;
            line.Condition = (ReturnLineCondition)lineModel.ConditionId;
            line.Comment = lineModel.Comment;
        }

        if (!string.IsNullOrWhiteSpace(model.AddSku))
        {
            var product = await productService.GetProductBySku(model.AddSku.Trim());
            if (product == null)
                errors.Add(translationService.GetResource("Admin.Inventory.Documents.Errors.SkuNotFound"));
            else if (!await productScope.HasAccess(product))
                errors.Add(translationService.GetResource("Admin.Catalog.Products.Permissions"));
            else if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                foreach (var combination in product.ProductAttributeCombinations)
                    document.Lines.Add(new InventoryDocumentLine
                        { ProductId = product.Id, Attributes = combination.Attributes });
            else
                document.Lines.Add(new InventoryDocumentLine { ProductId = product.Id });
        }

        return errors;
    }

    public async Task<IList<InventoryDocumentLine>> InaccessibleLines(InventoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var inaccessible = new List<InventoryDocumentLine>();
        foreach (var lines in document.Lines.GroupBy(x => x.ProductId))
        {
            var product = await productService.GetProductById(lines.Key);
            if (product == null || !await productScope.HasAccess(product))
                inaccessible.AddRange(lines);
        }

        return inaccessible;
    }

    public IList<string> Translate(IEnumerable<string> keys)
    {
        return keys.Select(translationService.GetResource).ToList();
    }
}
