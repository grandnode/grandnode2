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
    Task PrepareModel(InventoryDocumentModel model, InventoryDocument document, int linesPage = 1);

    /// <summary>
    ///     The warehouses the current user may put on a document
    /// </summary>
    Task<IList<Warehouse>> UsableWarehouses();

    /// <summary>
    ///     Writes the posted header and lines onto the draft
    /// </summary>
    /// <returns>The translated errors; the draft is not to be saved when there is one</returns>
    Task<IList<string>> ApplyModel(InventoryDocument document, InventoryDocumentModel model);

    /// <summary>
    ///     The products of the panel whose name or SKU holds the text, among those the document can book: their
    ///     stock managed, and for an assignment their batches or serial numbers tracked
    /// </summary>
    Task<IList<InventoryDocumentProductModel>> SearchProducts(InventoryDocument document, string keywords);

    /// <summary>
    ///     The combinations of a product stocked by attributes, with their stock in the document's warehouse; none for
    ///     another product
    /// </summary>
    Task<IList<InventoryDocumentCombinationModel>> ProductCombinations(InventoryDocument document, string productId);

    /// <summary>
    ///     Adds the product the picker chose: to the line already holding its bucket when it tracks no units, else as a
    ///     new line, a receipt's priced at the product's cost
    /// </summary>
    /// <returns>The translated errors, and the line added to last</returns>
    Task<(IList<string> Errors, string LineId)> AddLines(InventoryDocument document, InventoryDocumentModel model);

    /// <summary>
    ///     Adds the products chosen in the bulk popup, one of each and one of each combination; a product the document
    ///     cannot book, or the panel may not reach, is left out
    /// </summary>
    /// <returns>The names of the products left out, and the line added to last</returns>
    Task<(IList<string> LeftOut, string LineId)> AddProducts(InventoryDocument document, IList<string> productIds);

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
    //five form fields a line: a hundred stay well within the form's limit of 1024
    public const int LinesPageSize = 100;

    //a short list: the picker asks again as the name or SKU is typed
    private const int ProductsFound = 15;

    //the picker's value of a combination: the product's id and the combination's, which no id contains
    public const char CombinationSeparator = '|';

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

    public async Task PrepareModel(InventoryDocumentModel model, InventoryDocument document, int linesPage = 1)
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
            model.LinesPageCount = Math.Max(1, (document.Lines.Count + LinesPageSize - 1) / LinesPageSize);
            model.LinesPage = Math.Clamp(linesPage, 1, model.LinesPageCount);
            foreach (var line in document.Lines.Skip((model.LinesPage - 1) * LinesPageSize).Take(LinesPageSize))
            {
                var product = await productService.GetProductById(line.ProductId);
                model.Lines.Add(new InventoryDocumentLineModel {
                    Id = line.Id,
                    ProductId = product?.Id,
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
                    Comment = line.Comment,
                    TrackingMode = product != null && InventoryTracking.Tracked(product)
                        ? product.InventoryTrackingModeId
                        : InventoryTrackingMode.None,
                    BatchNumber = line.BatchNumber,
                    ExpiryDate = line.ExpiryDateUtc,
                    SerialNumbersText = string.Join("\n", line.SerialNumbers ?? [])
                });
            }
        }

        model.DocumentType = enumTranslationService.GetTranslationEnum((InventoryDocumentType)model.DocumentTypeId);
        model.AvailableWarehouses.Clear();
        model.AvailableWarehouses.Add(new SelectListItem { Text = "", Value = "" });
        foreach (var warehouse in await UsableWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = warehouse.Name, Value = warehouse.Id });

        //the pickers search categories and brands by name on the server; only those already chosen are rendered,
        //so a form shown again after an error keeps them
        if (document == null && model.DocumentTypeId == (int)InventoryDocumentType.StockCount)
        {
            model.AvailableCategories.Clear();
            foreach (var categoryId in model.CategoryIds ?? [])
                if (await categoryService.GetCategoryById(categoryId) is { } category)
                    model.AvailableCategories.Add(new SelectListItem {
                        Text = await categoryService.GetFormattedBreadCrumb(category), Value = category.Id,
                        Selected = true
                    });
            model.AvailableBrands.Clear();
            foreach (var brandId in model.BrandIds ?? [])
                if (await brandService.GetBrandById(brandId) is { } brand)
                    model.AvailableBrands.Add(new SelectListItem { Text = brand.Name, Value = brand.Id, Selected = true });
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
            await ApplyUnits(document, line, lineModel);
        }

        return errors;
    }

    /// <summary>
    ///     Whether the product's stock can be booked on the document: an assignment takes only products tracking
    ///     batches or serial numbers, as it gives them to stock without any
    /// </summary>
    private static bool Fits(InventoryDocument document, Product product)
    {
        return product.ManageInventoryMethodId != ManageInventoryMethod.DontManageStock &&
               (document.DocumentTypeId != InventoryDocumentType.Assignment || InventoryTracking.Tracked(product));
    }

    public async Task<IList<InventoryDocumentProductModel>> SearchProducts(InventoryDocument document,
        string keywords)
    {
        ArgumentNullException.ThrowIfNull(document);

        var found = new List<InventoryDocumentProductModel>();
        if (string.IsNullOrWhiteSpace(keywords))
            return found;

        var storeId = await productScope.GetDefaultStoreIdAsync() ?? "";
        var (products, _) = await productService.SearchProducts(CatalogAccess.IncludingHidden(storeId),
            pageSize: ProductsFound, keywords: keywords.Trim(), searchSku: true, orderBy: ProductSortingEnum.NameAsc);
        var text = keywords.Trim();
        foreach (var product in products)
        {
            if (!Fits(document, product) || !await productScope.HasAccess(product))
                continue;

            found.Add(new InventoryDocumentProductModel {
                Id = product.Id,
                Name = string.IsNullOrEmpty(product.Sku) ? product.Name : $"{product.Name} · {product.Sku}"
            });

            //a combination whose SKU was typed - or scanned - is offered on its own and added without asking
            if (product.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
                continue;
            foreach (var combination in product.ProductAttributeCombinations.Where(x =>
                         !string.IsNullOrEmpty(x.Sku) && x.Sku.Contains(text, StringComparison.OrdinalIgnoreCase)))
                found.Add(new InventoryDocumentProductModel {
                    Id = $"{product.Id}{CombinationSeparator}{combination.Id}",
                    Name = $"{product.Name} · " +
                           string.Join(", ", product.ParseProductAttributeValues(combination.Attributes).Select(x => x.Name)) +
                           $" · {combination.Sku}"
                });
        }

        return found;
    }

    public async Task<IList<InventoryDocumentCombinationModel>> ProductCombinations(InventoryDocument document,
        string productId)
    {
        ArgumentNullException.ThrowIfNull(document);

        var product = await productService.GetProductById(productId);
        if (product is not { ManageInventoryMethodId: ManageInventoryMethod.ManageStockByAttributes } ||
            !await productScope.HasAccess(product))
            return [];

        return product.ProductAttributeCombinations.Select(x => new InventoryDocumentCombinationModel {
            Id = x.Id,
            Text = string.Join(", ", product.ParseProductAttributeValues(x.Attributes).Select(v => v.Name)),
            Sku = x.Sku,
            Stock = StockOf(document, product, x)
        }).ToList();
    }

    public async Task<(IList<string> Errors, string LineId)> AddLines(InventoryDocument document,
        InventoryDocumentModel model)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(model);

        var product = await productService.GetProductById(model.AddProductId);
        if (product == null || product.ManageInventoryMethodId == ManageInventoryMethod.DontManageStock)
            return ([translationService.GetResource("Admin.Inventory.Documents.Errors.ProductNotFound")], null);
        if (!await productScope.HasAccess(product))
            return ([translationService.GetResource("Admin.Catalog.Products.Permissions")], null);
        if (!Fits(document, product))
            return ([translationService.GetResource(InventoryDocumentErrors.UnitsNotTracked)], null);

        InventoryDocumentLine added = null;
        if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
        {
            foreach (var addLine in model.AddLines.Where(x => x.Quantity > 0))
                if (product.ProductAttributeCombinations.FirstOrDefault(x => x.Id == addLine.CombinationId) is
                    { } combination)
                    added = AddLine(document, product, combination, addLine.Quantity);
        }
        else
        {
            //a product chosen in the picker is one of it; the quantity is typed on its line
            var quantity = model.AddLines.FirstOrDefault()?.Quantity is > 0 and var typed ? typed : 1;
            added = AddLine(document, product, null, quantity);
        }

        return ([], added?.Id);
    }

    public async Task<(IList<string> LeftOut, string LineId)> AddProducts(InventoryDocument document,
        IList<string> productIds)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(productIds);

        var leftOut = new List<string>();
        InventoryDocumentLine added = null;
        foreach (var productId in productIds.Distinct())
        {
            var product = await productService.GetProductById(productId);
            if (product == null)
                continue;
            if (!Fits(document, product) || !await productScope.HasAccess(product))
            {
                leftOut.Add(product.Name);
                continue;
            }

            if (product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
                foreach (var combination in product.ProductAttributeCombinations)
                    added = AddLine(document, product, combination, 1);
            else
                added = AddLine(document, product, null, 1);
        }

        return (leftOut, added?.Id);
    }

    /// <summary>
    ///     A product tracking no units adds to the line already holding its bucket; a batch or a set of serial numbers
    ///     is told apart only on its own line, so a product tracking them takes a new one
    /// </summary>
    private static InventoryDocumentLine AddLine(InventoryDocument document, Product product,
        ProductAttributeCombination combination, int quantity)
    {
        var line = InventoryTracking.Tracked(product)
            ? null
            : document.Lines.FirstOrDefault(x => x.ProductId == product.Id &&
                                                 (combination == null ||
                                                  ProductExtensions.AreProductAttributesEqual(product, x.Attributes,
                                                      combination.Attributes, false)));
        if (line == null)
        {
            line = new InventoryDocumentLine {
                ProductId = product.Id,
                ExpectedQuantity = Expected(document, product, combination),
                //what the stock is valued at now: the receipt changes it only when the price paid differs
                UnitCost = document.DocumentTypeId == InventoryDocumentType.Receipt
                    ? Math.Round((decimal)product.ProductCost, 2)
                    : null
            };
            if (combination != null)
                line.Attributes = combination.Attributes;
            document.Lines.Add(line);
        }

        if (document.DocumentTypeId == InventoryDocumentType.StockCount)
            line.CountedQuantity = (line.CountedQuantity ?? 0) + quantity;
        else
            line.Quantity += quantity;
        return line;
    }

    /// <summary>
    ///     The units a line names, for a product tracking them: its batch and expiry date, or its serial numbers, which
    ///     decide its quantity; a counted serial line counts the numbers scanned
    /// </summary>
    private async Task ApplyUnits(InventoryDocument document, InventoryDocumentLine line,
        InventoryDocumentLineModel lineModel)
    {
        var product = await productService.GetProductById(line.ProductId);
        if (product == null || !InventoryTracking.Tracked(product))
            return;

        if (product.InventoryTrackingModeId == InventoryTrackingMode.Batch)
        {
            line.BatchNumber = string.IsNullOrWhiteSpace(lineModel.BatchNumber) ? null : lineModel.BatchNumber.Trim();
            line.ExpiryDateUtc = lineModel.ExpiryDate.HasValue
                ? DateTime.SpecifyKind(lineModel.ExpiryDate.Value.Date, DateTimeKind.Utc)
                : null;
            return;
        }

        line.SerialNumbers = UnitNumbers.Parse(lineModel.SerialNumbersText);
        if (document.DocumentTypeId == InventoryDocumentType.StockCount)
        {
            if (lineModel.CountedQuantity.HasValue || line.SerialNumbers.Count > 0)
                line.CountedQuantity = line.SerialNumbers.Count;
        }
        else if (line.SerialNumbers.Count > 0 && document.DocumentTypeId != InventoryDocumentType.ReturnReceipt)
        {
            line.Quantity = lineModel.Quantity < 0 ? -line.SerialNumbers.Count : line.SerialNumbers.Count;
        }
    }

    /// <summary>
    ///     A stock count expects the stock the bucket holds now in the document's warehouse; other documents expect
    ///     nothing
    /// </summary>
    private static int? Expected(InventoryDocument document, Product product, ProductAttributeCombination combination)
    {
        return document.DocumentTypeId == InventoryDocumentType.StockCount
            ? StockOf(document, product, combination)
            : null;
    }

    /// <summary>
    ///     The stock the bucket holds now in the document's warehouse
    /// </summary>
    private static int StockOf(InventoryDocument document, Product product, ProductAttributeCombination combination)
    {
        if (!product.UseMultipleWarehouses)
            return combination?.StockQuantity ?? product.StockQuantity;

        return combination != null
            ? combination.WarehouseInventory.FirstOrDefault(x => x.WarehouseId == document.WarehouseId)?.StockQuantity ?? 0
            : product.ProductWarehouseInventory.FirstOrDefault(x => x.WarehouseId == document.WarehouseId)?.StockQuantity ?? 0;
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
