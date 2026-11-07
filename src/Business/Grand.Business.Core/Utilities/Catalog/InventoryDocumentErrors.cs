namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Why an inventory document cannot be saved, deleted or posted; each is the key of a translation resource
/// </summary>
public static class InventoryDocumentErrors
{
    public const string NotFound = "Admin.Inventory.Documents.Errors.NotFound";
    public const string Posted = "Admin.Inventory.Documents.Errors.Posted";
    public const string NoLines = "Admin.Inventory.Documents.Errors.NoLines";
    public const string WarehouseNotFound = "Admin.Inventory.Documents.Errors.WarehouseNotFound";
    public const string TargetWarehouseInvalid = "Admin.Inventory.Documents.Errors.TargetWarehouseInvalid";
    public const string ProductInOtherWarehouse = "Admin.Inventory.Documents.Errors.ProductInOtherWarehouse";

    public const string TransferNeedsMultipleWarehouses =
        "Admin.Inventory.Documents.Errors.TransferNeedsMultipleWarehouses";

    public const string QuantityMustBePositive = "Admin.Inventory.Documents.Errors.QuantityMustBePositive";
    public const string QuantityMustNotBeZero = "Admin.Inventory.Documents.Errors.QuantityMustNotBeZero";
    public const string UnitCostRequired = "Admin.Inventory.Documents.Errors.UnitCostRequired";
    public const string CommentRequired = "Admin.Inventory.Documents.Errors.CommentRequired";
    public const string CountedNegative = "Admin.Inventory.Documents.Errors.CountedNegative";
    public const string LineBooked = "Admin.Inventory.Documents.Errors.LineBooked";
    public const string DuplicateLine = "Admin.Inventory.Documents.Errors.DuplicateLine";
    public const string TooManyLines = "Admin.Inventory.Documents.Errors.TooManyLines";
    public const string ChangedWhilePosting = "Admin.Inventory.Documents.Errors.ChangedWhilePosting";
    public const string ReturnNotReceivable = "Admin.Inventory.Documents.Errors.ReturnNotReceivable";
    public const string ReturnQuantityExceeded = "Admin.Inventory.Documents.Errors.ReturnQuantityExceeded";
}
