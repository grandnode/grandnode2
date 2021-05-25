using Grand.Business.Common.Services.Security;
using Grand.Domain.Admin;
using System.Collections.Generic;

namespace Grand.Business.System.Utilities
{
    public static class StandardAdminSiteMap
    {
        public static readonly List<AdminSiteMap> SiteMap =
            new List<AdminSiteMap>() {
                new AdminSiteMap {
                    SystemName = "Dashboard",
                    ResourceName = "Admin.Dashboard",
                    ControllerName = "Home",
                    ActionName = "Index",
                    IconClass = "icon-home",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Dashboard",
                            ResourceName = "Admin.Dashboard",
                            ControllerName = "Home",
                            ActionName = "Index",
                            IconClass = "icon-bar-chart"
                        },
                        new AdminSiteMap {
                            SystemName = "Dashboard",
                            ResourceName = "Admin.Dashboard.Statistics",
                            ControllerName = "Home",
                            ActionName = "Statistics",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            IconClass = "icon-bulb"
                        },
                        new AdminSiteMap {
                            SystemName = "Shopping carts and wishlists",
                            ResourceName = "Admin.Dashboards.CurrentCartWishlists",
                            PermissionNames = new List<string> { PermissionSystemName.CurrentCarts },
                            ControllerName = "ShoppingCart",
                            ActionName = "CurrentCarts",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Online customers",
                            ResourceName = "Admin.Dashboards.OnlineCustomers",
                            PermissionNames = new List<string> { PermissionSystemName.Customers },
                            ControllerName = "OnlineCustomer",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Catalog",
                    ResourceName = "Admin.Catalog",
                    PermissionNames = new List<string> { PermissionSystemName.Products, PermissionSystemName.Categories, PermissionSystemName.Brands, PermissionSystemName.Collections, PermissionSystemName.ProductReviews, PermissionSystemName.ProductTags,
                         PermissionSystemName.ProductAttributes, PermissionSystemName.SpecificationAttributes },
                    IconClass = "fa fa-sitemap",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Products",
                            ResourceName = "Admin.Catalog.Products.Manage",
                            PermissionNames = new List<string> { PermissionSystemName.Products },
                            ControllerName = "Product",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Categories",
                            ResourceName = "Admin.Catalog.Categories",
                            ControllerName = "Category",
                            PermissionNames = new List<string> { PermissionSystemName.Categories },
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Brands",
                            ResourceName = "Admin.Catalog.Brands",
                            ControllerName = "Brand",
                            PermissionNames = new List<string> { PermissionSystemName.Brands },
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Collections",
                            ResourceName = "Admin.Catalog.Collections",
                            PermissionNames = new List<string> { PermissionSystemName.Collections },
                            ControllerName = "Collection",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Product tags",
                            ResourceName = "Admin.Catalog.ProductTags",
                            PermissionNames = new List<string> { PermissionSystemName.ProductTags },
                            ControllerName = "ProductTags",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Bulk edit products",
                            ResourceName = "Admin.Catalog.BulkEdit",
                            PermissionNames = new List<string> { PermissionSystemName.Products },
                            ControllerName = "Product",
                            ActionName = "BulkEdit",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Product reviews",
                            ResourceName = "Admin.Catalog.ProductReviews",
                            PermissionNames = new List<string> { PermissionSystemName.ProductReviews },
                            ControllerName = "ProductReview",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Products attributes",
                            ResourceName = "Admin.Catalog.Attributes.ProductAttributes",
                            ControllerName = "ProductAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.ProductAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Specification attributes",
                            ResourceName = "Admin.Catalog.Attributes.SpecificationAttributes",
                            ControllerName = "SpecificationAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.SpecificationAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                    }
                },
                new AdminSiteMap {
                    SystemName = "Sales",
                    ResourceName = "Admin.Sales",
                    PermissionNames = new List<string> { PermissionSystemName.Orders, PermissionSystemName.Shipments,
                        PermissionSystemName.MerchandiseReturns, PermissionSystemName.PaymentTransactions, PermissionSystemName.OrderTags,
                        PermissionSystemName.OrderStatus, PermissionSystemName.CheckoutAttributes },
                    IconClass = "icon-basket",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Orders",
                            ResourceName = "Admin.Orders",
                            PermissionNames = new List<string> { PermissionSystemName.Orders },
                            ControllerName = "Order",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Shipments",
                            ResourceName = "Admin.Orders.Shipments.List",
                            PermissionNames = new List<string> { PermissionSystemName.Shipments },
                            ControllerName = "Shipment",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Merchandise returns",
                            ResourceName = "Admin.Orders.MerchandiseReturns",
                            PermissionNames = new List<string> { PermissionSystemName.MerchandiseReturns },
                            ControllerName = "MerchandiseReturn",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Payment transaction",
                            ResourceName = "Admin.Orders.PaymentTransaction",
                            PermissionNames = new List<string> { PermissionSystemName.PaymentTransactions },
                            ControllerName = "PaymentTransaction",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "OrderTags",
                            ResourceName = "Admin.Orders.OrderTags",
                            PermissionNames = new List<string> { PermissionSystemName.OrderTags },
                            ControllerName = "OrderTags",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Checkout attributes",
                            ResourceName = "Admin.Orders.CheckoutAttributes",
                            ControllerName = "CheckoutAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.CheckoutAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Order status",
                            ResourceName = "Admin.Orders.OrderStatus",
                            ControllerName = "OrderStatus",
                            PermissionNames = new List<string> { PermissionSystemName.OrderStatus },
                            ActionName = "Index",
                            IconClass = "fa fa-dot-circle-o"
                        },

                    }
                },
                new AdminSiteMap {
                    SystemName = "Customers",
                    ResourceName = "Admin.Customers",
                    PermissionNames = new List<string> { PermissionSystemName.Customers, PermissionSystemName.Vendors, PermissionSystemName.VendorReviews, PermissionSystemName.ActivityLog,
                        PermissionSystemName.CustomerTags, PermissionSystemName.CustomerGroups, PermissionSystemName.SalesEmployees },
                    IconClass = "icon-users",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Customers",
                            ResourceName = "Admin.Customers.Customers",
                            PermissionNames = new List<string> { PermissionSystemName.Customers },
                            ControllerName = "Customer",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Customer groups",
                            ResourceName = "Admin.Customers.CustomerGroups",
                            PermissionNames = new List<string> { PermissionSystemName.CustomerGroups },
                            ControllerName = "CustomerGroup",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Customer tags",
                            ResourceName = "Admin.Customers.CustomerTags",
                            PermissionNames = new List<string> { PermissionSystemName.CustomerTags },
                            ControllerName = "CustomerTag",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Vendors",
                            ResourceName = "Admin.Vendors",
                            PermissionNames = new List<string> { PermissionSystemName.Vendors },
                            ControllerName = "Vendor",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Vendor reviews",
                            ResourceName = "Admin.VendorReviews",
                            PermissionNames = new List<string> { PermissionSystemName.VendorReviews },
                            ControllerName = "VendorReview",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Sales employee",
                            ResourceName = "Admin.Customers.SalesEmployees",
                            PermissionNames = new List<string> { PermissionSystemName.SalesEmployees },
                            ControllerName = "SalesEmployee",
                            ActionName = "Index",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Customer attributes",
                            ResourceName = "Admin.Customers.Attributes.Customer",
                            ControllerName = "CustomerAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.CustomerAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Address attributes",
                            ResourceName = "Admin.Customers.Attributes.Address",
                            ControllerName = "AddressAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.AddressAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Activity Log",
                            ResourceName = "Admin.Configuration.ActivityLog",
                            PermissionNames = new List<string> { PermissionSystemName.ActivityLog },
                            ControllerName = "ActivityLog",
                            ActionName = "ListLogs",
                            IconClass = "fa fa-arrow-circle-o-right"
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Marketing",
                    ResourceName = "Admin.Marketing",
                    PermissionNames = new List<string> { PermissionSystemName.Affiliates, PermissionSystemName.NewsletterCategories, PermissionSystemName.NewsletterSubscribers,
                        PermissionSystemName.Campaigns, PermissionSystemName.Discounts, PermissionSystemName.Actions, PermissionSystemName.Reminders, PermissionSystemName.PushNotifications,
                        PermissionSystemName.Banners, PermissionSystemName.InteractiveForms, PermissionSystemName.Affiliates, PermissionSystemName.Documents, PermissionSystemName.GiftVouchers,
                        PermissionSystemName.ContactAttributes},
                    IconClass = "icon-bulb",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Discounts",
                            ResourceName = "Admin.Marketing.Discounts",
                            PermissionNames = new List<string> { PermissionSystemName.Discounts },
                            ControllerName = "Discount",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Gift vouchers",
                            ResourceName = "Admin.GiftVouchers",
                            PermissionNames = new List<string> { PermissionSystemName.GiftVouchers },
                            ControllerName = "GiftVoucher",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Customer reminders",
                            ResourceName = "Admin.Customers.CustomerReminders",
                            PermissionNames = new List<string> { PermissionSystemName.Reminders },
                            ControllerName = "CustomerReminder",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Affiliates",
                            ResourceName = "Admin.Affiliates",
                            PermissionNames = new List<string> { PermissionSystemName.Affiliates },
                            ControllerName = "Affiliate",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Contact attributes",
                            ResourceName = "Admin.Catalog.Attributes.ContactAttributes",
                            ControllerName = "ContactAttribute",
                            ActionName = "List",
                            PermissionNames = new List<string> { PermissionSystemName.ContactAttributes },
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Newsletter",
                            ResourceName = "Admin.Marketing.Newsletter",
                            PermissionNames = new List<string> { PermissionSystemName.Campaigns, PermissionSystemName.NewsletterSubscribers },
                            IconClass = "fa fa-dot-circle-o",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Campaigns",
                                    ResourceName = "Admin.Marketing.Campaigns",
                                    PermissionNames = new List<string> { PermissionSystemName.Campaigns },
                                    ControllerName = "Campaign",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Newsletter categories",
                                    ResourceName = "Admin.Marketing.NewsletterCategory",
                                    PermissionNames = new List<string> { PermissionSystemName.NewsletterCategories },
                                    ControllerName = "NewsletterCategory",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Newsletter subscriptions",
                                    ResourceName = "Admin.Marketing.NewsletterSubscriptions",
                                    PermissionNames = new List<string> { PermissionSystemName.NewsletterSubscribers },
                                    ControllerName = "NewsLetterSubscription",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },                      
                        new AdminSiteMap {
                            SystemName = "Customer actions",
                            ResourceName = "Admin.Customers.CustomerActions",
                            PermissionNames = new List<string> { PermissionSystemName.Actions, PermissionSystemName.Banners, PermissionSystemName.InteractiveForms },
                            IconClass = "fa fa-dot-circle-o",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Customer action type",
                                    ResourceName = "Admin.Customers.Actiontype",
                                    PermissionNames = new List<string> { PermissionSystemName.Actions },
                                    ControllerName = "CustomerActionType",
                                    ActionName = "ListTypes",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Customer actions",
                                    ResourceName = "Admin.Customers.CustomerActions",
                                    PermissionNames = new List<string> { PermissionSystemName.Actions },
                                    ControllerName = "CustomerAction",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Banners",
                                    ResourceName = "Admin.Marketing.Banners",
                                    PermissionNames = new List<string> { PermissionSystemName.Banners },
                                    ControllerName = "Banner",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "InteractiveForms",
                                    ResourceName = "Admin.Marketing.InteractiveForms",
                                    PermissionNames = new List<string> { PermissionSystemName.InteractiveForms },
                                    ControllerName = "InteractiveForm",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "PushNotifications",
                            ResourceName = "Admin.PushNotifications",
                            PermissionNames = new List<string> { PermissionSystemName.PushNotifications },
                            ControllerName = "PushNotifications",
                            IconClass = "fa fa-dot-circle-o",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "PushNotifications",
                                    ResourceName = "Admin.PushNotifications.Send",
                                    PermissionNames = new List<string> { PermissionSystemName.PushNotifications },
                                    ControllerName = "PushNotifications",
                                    ActionName = "Send",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "PushNotifications",
                                    ResourceName = "Admin.PushNotifications.Messages",
                                    PermissionNames = new List<string> { PermissionSystemName.PushNotifications },
                                    ControllerName = "PushNotifications",
                                    ActionName = "Messages",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "PushNotifications",
                                    ResourceName = "Admin.PushNotifications.Receivers",
                                    PermissionNames = new List<string> { PermissionSystemName.PushNotifications },
                                    ControllerName = "PushNotifications",
                                    ActionName = "Receivers",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "Documents",
                            ResourceName = "Admin.Marketing.Documents",
                            PermissionNames = new List<string> { PermissionSystemName.Documents },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Document types",
                                    ResourceName = "Admin.Marketing.Document.Type",
                                    ControllerName = "Document",
                                    ActionName = "Types",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Document list",
                                    ResourceName = "Admin.Marketing.Document.List",
                                    ControllerName = "Document",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                    }
                },
                new AdminSiteMap {
                    SystemName = "Content",
                    ResourceName = "Admin.Content",
                    PermissionNames = new List<string> { PermissionSystemName.News, PermissionSystemName.Blog,
                        PermissionSystemName.Pages, PermissionSystemName.MessageTemplates,
                        PermissionSystemName.Knowledgebase, PermissionSystemName.Courses },
                    IconClass = "icon-layers",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Pages",
                            ResourceName = "Admin.Content.Pages",
                            PermissionNames = new List<string> { PermissionSystemName.Pages },
                            ControllerName = "Page",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Message templates",
                            ResourceName = "Admin.Content.MessageTemplates",
                            PermissionNames = new List<string> { PermissionSystemName.MessageTemplates },
                            ControllerName = "MessageTemplate",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Knowledgebase",
                            ResourceName = "Admin.Content.Knowledgebase",
                            PermissionNames = new List<string> { PermissionSystemName.Knowledgebase },
                            ControllerName = "Knowledgebase",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "News",
                            ResourceName = "Admin.Content.News",
                            PermissionNames = new List<string> { PermissionSystemName.News },
                            ControllerName = "News",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Blog",
                            ResourceName = "Admin.Content.Blog",
                            PermissionNames = new List<string> { PermissionSystemName.Blog },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Blog categories",
                                    ResourceName = "Admin.Content.Blog.BlogCategories",
                                    ControllerName = "Blog",
                                    ActionName = "CategoryList",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Blog posts",
                                    ResourceName = "Admin.Content.Blog.BlogPosts",
                                    ControllerName = "Blog",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Blog comments",
                                    ResourceName = "Admin.Content.Blog.Comments",
                                    ControllerName = "Blog",
                                    ActionName = "Comments",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "Course",
                            ResourceName = "Admin.Content.Course",
                            PermissionNames = new List<string> { PermissionSystemName.Courses },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Course level",
                                    ResourceName = "Admin.Content.Course.Level",
                                    ControllerName = "Course",
                                    ActionName = "Level",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Manage courses",
                                    ResourceName = "Admin.Content.Course.Manage",
                                    ControllerName = "Course",
                                    ActionName = "List",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Reports",
                    ResourceName = "Admin.Reports",
                    PermissionNames = new List<string> { PermissionSystemName.Reports, PermissionSystemName.ActivityLog },
                    IconClass = "icon-bar-chart",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Low stock report",
                            ResourceName = "Admin.Reports.LowStockReport",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            ControllerName = "Reports",
                            ActionName = "LowStockReport",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Bestsellers",
                            ResourceName = "Admin.Reports.Bestsellers",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            ControllerName = "Reports",
                            ActionName = "BestsellersReport",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Products never purchased",
                            ResourceName = "Admin.Reports.NeverSold",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            ControllerName = "Reports",
                            ActionName = "NeverSoldReport",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Country report",
                            ResourceName = "Admin.Reports.Country",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            AllPermissions = true,
                            ControllerName = "Reports",
                            ActionName = "CountryReport",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Customer reports",
                            ResourceName = "Admin.Reports.Customers",
                            PermissionNames = new List<string> { PermissionSystemName.Reports },
                            AllPermissions = true,
                            ControllerName = "Reports",
                            ActionName = "Customer",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Activity Stats",
                            ResourceName = "Admin.Reports.ActivityLog.ActivityStats",
                            PermissionNames = new List<string> { PermissionSystemName.ActivityLog },
                            ControllerName = "ActivityLog",
                            ActionName = "ListStats",
                            IconClass = "fa fa-dot-circle-o"
                         }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Configuration",
                    ResourceName = "Admin.Configuration",
                    PermissionNames = new List<string> { PermissionSystemName.Countries, PermissionSystemName.Languages, PermissionSystemName.Settings,
                        PermissionSystemName.PaymentMethods, PermissionSystemName.ExternalAuthenticationMethods,
                        PermissionSystemName.TaxSettings, PermissionSystemName.ShippingSettings, PermissionSystemName.Currencies, PermissionSystemName.Measures,
                        PermissionSystemName.ActivityLog, PermissionSystemName.Acl, PermissionSystemName.EmailAccounts, PermissionSystemName.Plugins, PermissionSystemName.Widgets, PermissionSystemName.Stores, PermissionSystemName.Maintenance },
                    IconClass = "icon-settings",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Stores",
                            ResourceName = "Admin.Configuration.Stores",
                            PermissionNames = new List<string> { PermissionSystemName.Stores },
                            ControllerName = "Store",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Permissions",
                            ResourceName = "Admin.Configuration.Permissions",
                            PermissionNames = new List<string> { PermissionSystemName.Acl },
                            ControllerName = "Permission",
                            ActionName = "Index",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "EmailAccounts",
                            ResourceName = "Admin.Configuration.EmailAccounts",
                            PermissionNames = new List<string> { PermissionSystemName.EmailAccounts },
                            ControllerName = "EmailAccount",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Languages",
                            ResourceName = "Admin.Configuration.Languages",
                            PermissionNames = new List<string> { PermissionSystemName.Languages },
                            ControllerName = "Language",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Currencies",
                            ResourceName = "Admin.Configuration.Currencies",
                            PermissionNames = new List<string> { PermissionSystemName.Currencies },
                            ControllerName = "Currency",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Countries",
                            ResourceName = "Admin.Configuration.Countries",
                            PermissionNames = new List<string> { PermissionSystemName.Countries },
                            ControllerName = "Country",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Search engine friendly names",
                            ResourceName = "Admin.Configuration.SeNames",
                            PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                            ControllerName = "Common",
                            ActionName = "SeNames",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Payment",
                            ResourceName = "Admin.Configuration.Payment",
                            PermissionNames = new List<string> { PermissionSystemName.PaymentMethods },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Payment methods",
                                    ResourceName = "Admin.Configuration.Payment.Methods",
                                    PermissionNames = new List<string> { PermissionSystemName.PaymentMethods },
                                    ControllerName = "Payment",
                                    ActionName = "Index",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Payment settings",
                                    ResourceName = "Admin.Configuration.Payment.Settings",
                                    PermissionNames = new List<string> { PermissionSystemName.PaymentMethods },
                                    ControllerName = "Payment",
                                    ActionName = "Settings",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Payment method restrictions",
                                    ResourceName = "Admin.Configuration.Payment.MethodRestrictions",
                                    PermissionNames = new List<string> { PermissionSystemName.PaymentMethods },
                                    ControllerName = "Payment",
                                    ActionName = "MethodRestrictions",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "Shipping",
                            ResourceName = "Admin.Configuration.Shipping",
                            PermissionNames = new List<string> { PermissionSystemName.ShippingSettings },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Shipping providers",
                                    ResourceName = "Admin.Configuration.Shipping.Providers",
                                    ControllerName = "Shipping",
                                    ActionName = "Providers",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Shipping methods",
                                    ResourceName = "Admin.Configuration.Shipping.Methods",
                                    ControllerName = "Shipping",
                                    ActionName = "Methods",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Shipping method restrictions",
                                    ResourceName = "Admin.Configuration.Shipping.Restrictions",
                                    ControllerName = "Shipping",
                                    ActionName = "Restrictions",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Shipping settings",
                                    ResourceName = "Admin.Configuration.Shipping.Settings" +
                                    "",
                                    ControllerName = "Shipping",
                                    ActionName = "Settings",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                        new AdminSiteMap {
                            SystemName = "Measures",
                            ResourceName = "Admin.Configuration.Measures",
                            PermissionNames = new List<string> { PermissionSystemName.Measures },
                            ControllerName = "Measure",
                            ActionName = "Index",
                            IconClass = "fa fa-arrow-circle-o-right",
                        },
                                new AdminSiteMap {
                                    SystemName = "Delivery dates",
                                    ResourceName = "Admin.Configuration.Shipping.DeliveryDates",
                                    ControllerName = "Shipping",
                                    ActionName = "DeliveryDates",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Warehouses",
                                    ResourceName = "Admin.Configuration.Shipping.Warehouses",
                                    ControllerName = "Shipping",
                                    ActionName = "Warehouses",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "PickupPoints",
                                    ResourceName = "Admin.Configuration.Shipping.PickupPoints",
                                    ControllerName = "Shipping",
                                    ActionName = "PickupPoints",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "Tax",
                            ResourceName = "Admin.Configuration.Tax",
                            PermissionNames = new List<string> { PermissionSystemName.TaxSettings },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Tax providers",
                                    ResourceName = "Admin.Configuration.Tax.Providers",
                                    ControllerName = "Tax",
                                    ActionName = "Providers",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Tax categories",
                                    ResourceName = "Admin.Configuration.Tax.Categories",
                                    ControllerName = "Tax",
                                    ActionName = "Categories",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Tax settings",
                                    ResourceName = "Admin.Configuration.Tax.Settings",
                                    ControllerName = "Tax",
                                    ActionName = "Settings",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        },
                        new AdminSiteMap {
                            SystemName = "Templates",
                            ResourceName = "Admin.Configuration.Layouts",
                            PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                            IconClass = "fa fa-arrow-circle-o-right",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Category layouts",
                                    ResourceName = "Admin.Configuration.Layouts.Category",
                                    ControllerName = "Layout",
                                    ActionName = "CategoryLayouts",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Brand layouts",
                                    ResourceName = "Admin.Configuration.Layouts.Brand",
                                    ControllerName = "Layout",
                                    ActionName = "BrandLayouts",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Collection layouts",
                                    ResourceName = "Admin.Configuration.Layouts.Collection",
                                    ControllerName = "Layout",
                                    ActionName = "CollectionLayouts",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Product layouts",
                                    ResourceName = "Admin.Configuration.Layouts.Product",
                                    ControllerName = "Layout",
                                    ActionName = "ProductLayouts",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Page layouts",
                                    ResourceName = "Admin.Configuration.Layouts.Page",
                                    ControllerName = "Layout",
                                    ActionName = "PageLayouts",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Settings",
                    PermissionNames = new List<string> { PermissionSystemName.Settings, PermissionSystemName.ActivityLog },
                    ResourceName = "Admin.Settings",
                    IconClass = "icon-wrench",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                                    SystemName = "General and common settings",
                                    ResourceName = "Admin.Settings.GeneralCommon",
                                    ControllerName = "Setting",
                                    ActionName = "GeneralCommon",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Catalog settings",
                                    ResourceName = "Admin.Settings.Catalog",
                                    ControllerName = "Setting",
                                    ActionName = "Catalog",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Customer settings",
                                    ResourceName = "Admin.Settings.Customer",
                                    ControllerName = "Setting",
                                    ActionName = "Customer",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Sales settings",
                                    ResourceName = "Admin.Settings.Sales",
                                    ControllerName = "Setting",
                                    ActionName = "Sales",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Media settings",
                                    ResourceName = "Admin.Settings.Media",
                                    ControllerName = "Setting",
                                    ActionName = "Media",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Content settings",
                                    ResourceName = "Admin.Settings.Content",
                                    ControllerName = "Setting",
                                    ActionName = "Content",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Vendor settings",
                                    ResourceName = "Admin.Settings.Vendor",
                                    ControllerName = "Setting",
                                    ActionName = "Vendor",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Push notifications settings",
                                    ResourceName = "Admin.Settings.PushNotifications",
                                    ControllerName = "Setting",
                                    ActionName = "PushNotifications",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Admin search settings",
                                    ResourceName = "Admin.Settings.AdminSearch",
                                    ControllerName = "Setting",
                                    ActionName = "AdminSearch",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "System settings",
                                    ResourceName = "Admin.Settings.System",
                                    ControllerName = "Setting",
                                    ActionName = "SystemSetting",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Activity Types",
                                    ResourceName = "Admin.Settings.ActivityLog.ActivityLogType",
                                    PermissionNames = new List<string> { PermissionSystemName.ActivityLog },
                                    ControllerName = "ActivityLog",
                                    ActionName = "ListTypes",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Plugins",
                    ResourceName = "Admin.Plugins",
                    PermissionNames = new List<string> { PermissionSystemName.Plugins, PermissionSystemName.Widgets, PermissionSystemName.ExternalAuthenticationMethods},
                    IconClass = "icon-puzzle",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "External authentication methods",
                            ResourceName = "Admin.Plugins.ExternalAuthenticationMethods",
                            PermissionNames = new List<string> { PermissionSystemName.ExternalAuthenticationMethods },
                            ControllerName = "ExternalAuthentication",
                            ActionName = "Methods",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Widgets",
                            ResourceName = "Admin.Plugins.Widgets",
                            PermissionNames = new List<string> { PermissionSystemName.Widgets },
                            ControllerName = "Widget",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Local plugins",
                            ResourceName = "Admin.Plugins.Local",
                            PermissionNames = new List<string> { PermissionSystemName.Plugins },
                            ControllerName = "Plugin",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "System",
                    ResourceName = "Admin.System",
                    PermissionNames = new List<string> { PermissionSystemName.SystemLog, PermissionSystemName.MessageQueue, PermissionSystemName.MessageContactForm,
                        PermissionSystemName.Maintenance, PermissionSystemName.ScheduleTasks },
                    IconClass = "icon-info",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Log",
                            ResourceName = "Admin.System.Log",
                            PermissionNames = new List<string> { PermissionSystemName.SystemLog },
                            ControllerName = "Logger",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "System information",
                            ResourceName = "Admin.System.SystemInfo",
                            PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                            ControllerName = "Common",
                            ActionName = "SystemInfo",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Queued emails",
                            ResourceName = "Admin.System.QueuedEmails",
                            PermissionNames = new List<string> { PermissionSystemName.MessageQueue },
                            ControllerName = "QueuedEmail",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Contact Us form",
                            ResourceName = "Admin.System.ContactForm",
                            PermissionNames = new List<string> { PermissionSystemName.MessageContactForm },
                            ControllerName = "ContactForm",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Maintenance",
                            ResourceName = "Admin.System.Maintenance",
                            PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                            ControllerName = "Common",
                            ActionName = "Maintenance",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Schedule tasks",
                            ResourceName = "Admin.System.ScheduleTasks",
                            PermissionNames = new List<string> { PermissionSystemName.ScheduleTasks },
                            ControllerName = "ScheduleTask",
                            ActionName = "List",
                            IconClass = "fa fa-dot-circle-o"
                        },
                        new AdminSiteMap {
                            SystemName = "Developer tools",
                            ResourceName = "Admin.System.DeveloperTools",
                            PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                            IconClass = "fa fa-dot-circle-o",
                            ChildNodes = new List<AdminSiteMap>() {
                                new AdminSiteMap {
                                    SystemName = "Manage API Users",
                                    ResourceName = "Admin.System.APIUsers",
                                    PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                                    ControllerName = "ApiUser",
                                    ActionName = "Index",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Roslyn compiler",
                                    ResourceName = "Admin.System.Roslyn",
                                    PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                                    ControllerName = "Common",
                                    ActionName = "Roslyn",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Custom css",
                                    ResourceName = "Admin.System.CustomCss",
                                    PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                                    ControllerName = "Common",
                                    ActionName = "CustomCss",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Custom JS",
                                    ResourceName = "Admin.System.CustomJs",
                                    PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                                    ControllerName = "Common",
                                    ActionName = "CustomJs",
                                    IconClass = "fa fa-dot-circle-o"
                                },
                                new AdminSiteMap {
                                    SystemName = "Robot.txt",
                                    ResourceName = "Admin.System.AdditionsRobotsTxt",
                                    PermissionNames = new List<string> { PermissionSystemName.Maintenance },
                                    ControllerName = "Common",
                                    ActionName = "AdditionsRobotsTxt",
                                    IconClass = "fa fa-dot-circle-o"
                                }
                            }
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Help",
                    ResourceName = "Admin.Help",
                    IconClass = "icon-question",
                    ChildNodes = new List<AdminSiteMap>() {
                        new AdminSiteMap {
                            SystemName = "Community forums",
                            ResourceName = "Admin.Help.Forums",
                            Url = "https://grandnode.com/boards?utm_source=web&utm_medium=admin&utm_term=web&utm_campaign=Community",
                            IconClass = "fa fa-dot-circle-o",
                            OpenUrlInNewTab = true
                        },
                        new AdminSiteMap {
                            SystemName = "Premium support services",
                            ResourceName = "Admin.Help.SupportServices",
                            Url = "https://grandnode.com/premium-support-packages?utm_source=web&utm_medium=admin&utm_term=web&utm_campaign=Support",
                            IconClass = "fa fa-dot-circle-o",
                            OpenUrlInNewTab = true
                        }
                    }
                },
                new AdminSiteMap {
                    SystemName = "Third party plugins",
                    ResourceName = "Admin.Plugins"
                }

            };
    };
}