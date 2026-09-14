//Representative grid pages whose requests are recorded as HAR for request parity
//checks when the grids are replaced. Each entry covers a different grid shape.
//`tab` opens an <admin-tabstrip> tab (index) before waiting for the grid.
//`captureUpdate` opens inline edit, clicks Update without changing anything and
//answers the POST in the browser (route.fulfill), so the serialised payload is
//recorded while the server never receives it.
export const HAR_PAGES = [
    { panel: 'admin', name: 'product-list', path: '/Admin/Product/List', grid: 'products-grid' },
    { panel: 'admin', name: 'category-list', path: '/Admin/Category/List', grid: 'categories-grid' },
    { panel: 'admin', name: 'order-list', path: '/Admin/Order/List', grid: 'orders-grid' },
    { panel: 'admin', name: 'customer-list', path: '/Admin/Customer/List', grid: 'customers-grid' },
    { panel: 'admin', name: 'current-carts', path: '/Admin/ShoppingCart/CurrentCarts', grid: 'carts-grid', detail: true },
    { panel: 'admin', name: 'measure-weights', path: '/Admin/Measure/Index', tab: ['measures-list', 2], grid: 'measureweight-grid', captureUpdate: /\/Measure\/WeightUpdate/i },
    { panel: 'admin', name: 'currency-list', path: '/Admin/Currency/List', grid: 'currencies-grid', captureUpdate: /\/Currency\/\w*Update/i },
    { panel: 'admin', name: 'tax-categories', path: '/Admin/Tax/Categories', grid: 'tax-categories-grid', captureUpdate: /\/Tax\/CategoryUpdate/i },
    { panel: 'admin', name: 'language-list', path: '/Admin/Language/List', grid: 'languages-grid' },
    { panel: 'admin', name: 'country-list', path: '/Admin/Country/List', grid: 'countries-grid' },
    { panel: 'admin', name: 'message-templates', path: '/Admin/MessageTemplate/List', grid: 'templates-grid' },
    { panel: 'admin', name: 'discount-list', path: '/Admin/Discount/List', grid: 'discounts-grid' },
    { panel: 'admin', name: 'bestsellers-report', path: '/Admin/Reports/BestsellersReport', grid: 'salesreport-grid' },
    { panel: 'admin', name: 'shipping-byweight', path: '/Admin/ShippingByWeight/Configure', grid: 'shipping-byweight-grid', optional: true },
    { panel: 'store', name: 'product-list', path: '/Store/Product/List', grid: 'products-grid' },
    { panel: 'store', name: 'order-list', path: '/Store/Order/List', grid: 'orders-grid' },
    { panel: 'store', name: 'currency-list', path: '/Store/Currency/List', grid: 'currencies-grid' },
    { panel: 'store', name: 'tax-categories', path: '/Store/Tax/Categories', grid: 'tax-categories-grid', captureUpdate: /\/Tax\/CategoryUpdate/i },
    { panel: 'vendor', name: 'product-list', path: '/Vendor/Product/List', grid: 'products-grid' },
    { panel: 'vendor', name: 'shipment-list', path: '/Vendor/Shipment/List', grid: 'shipments-grid', detail: true },
    { panel: 'vendor', name: 'bestsellers-report', path: '/Vendor/Reports/BestsellersReport', grid: 'salesreport-grid' }
]
