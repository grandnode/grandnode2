namespace Grand.Web.Vendor.Models.Catalog;

/// <summary>
///     Implement on any Vendor-area POST model that carries a product id. The global
///     <see cref="Grand.Infrastructure.Validators.ValidationFilter" /> resolves an
///     <c>IValidator&lt;IProductValidVendor&gt;</c> for every such model and rejects the request unless
///     <see cref="ProductId" /> belongs to the current vendor - see
///     Grand.Web.Vendor.Validators.Catalog.ProductValidVendor.
///     IMPORTANT: if the model carries more than one product-id-shaped field (e.g. an owning/parent id
///     plus a referenced/component id), make sure <see cref="ProductId" /> is bound to whichever id the
///     action actually mutates. A mismatch silently authorizes the wrong product - see
///     ProductModel.BundleProductModel (ProductId vs ProductBundleId) for a bug of this exact shape that
///     was found and fixed.
/// </summary>
public interface IProductValidVendor
{
    public string ProductId { get; set; }
}

/// <summary>
///     Implement on Vendor-area POST models that relate two products (e.g. related/similar products).
///     The paired validator (ProductRelatedValidVendor) requires ownership of <see cref="ProductId1" />
///     only, because the consuming actions only ever read/mutate ProductId1's mapping list. Do not widen
///     this to accept ownership of either id ("OR") unless the action is also changed to only ever
///     mutate whichever product is actually owned - an OR check let an attacker satisfy validation via
///     ProductId2 while mutating a ProductId1 they don't own.
/// </summary>
public interface IProductRelatedValidVendor
{
    public string ProductId1 { get; set; }
    public string ProductId2 { get; set; }
}