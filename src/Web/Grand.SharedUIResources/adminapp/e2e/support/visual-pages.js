//Pages whose look is compared before and after a stylesheet change (npm run e2e:visual).
//The set is chosen for what the Kendo stylesheets used to paint: the `k-button` and
//`k-link` classes the views still carry, the `k-icon` glyphs, the grids, the tab strips
//and the plugin configuration screens.
//
//An entry is either a plain `path`, or a `lookup` that reads the first row of a list
//grid and builds the URL of an edit page from it, so no record id is hard-coded.
export const VISUAL_PAGES = [
    { panel: 'admin', name: 'product-list', path: '/Admin/Product/List', grid: 'products-grid' },
    {
        panel: 'admin', name: 'product-edit', tabs: ['product-edit', 0],
        lookup: { path: '/Admin/Product/List', grid: 'products-grid', url: id => `/Admin/Product/Edit/${id}` }
    },
    { panel: 'admin', name: 'order-list', path: '/Admin/Order/List', grid: 'orders-grid' },
    {
        panel: 'admin', name: 'order-details',
        lookup: { path: '/Admin/Order/List', grid: 'orders-grid', url: id => `/Admin/Order/Edit/${id}` }
    },
    { panel: 'admin', name: 'customer-list', path: '/Admin/Customer/List', grid: 'customers-grid' },
    {
        panel: 'admin', name: 'customer-edit', tabs: ['customer-edit', 0],
        lookup: { path: '/Admin/Customer/List', grid: 'customers-grid', url: id => `/Admin/Customer/Edit/${id}` }
    },
    { panel: 'admin', name: 'measures', path: '/Admin/Measure/Index', tabs: ['measures-list', 2], grid: 'measureweight-grid' },
    { panel: 'admin', name: 'tax-categories', path: '/Admin/Tax/Categories', grid: 'tax-categories-grid' },
    { panel: 'admin', name: 'vendor-reviews', path: '/Admin/VendorReview/List', grid: 'vendorreviews-grid' },
    { panel: 'admin', name: 'message-templates', path: '/Admin/MessageTemplate/List', grid: 'templates-grid' },
    { panel: 'admin', name: 'gift-vouchers', path: '/Admin/GiftVoucher/List', grid: 'giftvouchers-grid' },
    { panel: 'admin', name: 'setting-catalog', path: '/Admin/Setting/Catalog' },
    { panel: 'admin', name: 'language-resources', path: '/Admin/Language/List', grid: 'languages-grid' },
    { panel: 'admin', name: 'shipping-byweight', path: '/Admin/ShippingByWeight/Configure', grid: 'shipping-byweight-grid', optional: true },
    { panel: 'admin', name: 'shipping-point', path: '/Admin/ShippingPoint/Configure', grid: 'shipping-points-grid', optional: true },
    { panel: 'admin', name: 'tax-countrystatezip', path: '/Admin/TaxCountryStateZip/Configure', grid: 'tax-countrystatezip-grid', optional: true },
    { panel: 'admin', name: 'widgets-slider', path: '/Admin/WidgetsSlider/Configure', grid: 'slider-grid', optional: true },

    { panel: 'store', name: 'product-list', path: '/Store/Product/List', grid: 'products-grid' },
    { panel: 'store', name: 'order-list', path: '/Store/Order/List', grid: 'orders-grid' },
    { panel: 'store', name: 'tax-categories', path: '/Store/Tax/Categories', grid: 'tax-categories-grid' },
    { panel: 'store', name: 'shipping-byweight', path: '/Store/ShippingByWeight/Configure', grid: 'shipping-byweight-grid', optional: true },

    { panel: 'vendor', name: 'product-list', path: '/Vendor/Product/List', grid: 'products-grid' },
    { panel: 'vendor', name: 'shipment-list', path: '/Vendor/Shipment/List', grid: 'shipments-grid' },
    {
        panel: 'vendor', name: 'shipment-details',
        lookup: { path: '/Vendor/Shipment/List', grid: 'shipments-grid', url: id => `/Vendor/Shipment/ShipmentDetails/${id}` }
    }
]
