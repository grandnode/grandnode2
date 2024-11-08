using Grand.Domain.Customers;
using Grand.Domain.Permissions;

namespace Grand.Module.Installer.Extensions;

public static class PermissionExtensions
{
    public static IEnumerable<Permission> Permissions()
    {
        return [
            StandardPermission.ManageAccessAdminPanel,
            StandardPermission.ManageAccessVendorPanel,
            StandardPermission.AllowCustomerImpersonation,
            StandardPermission.ManageProducts,
            StandardPermission.ManageCategories,
            StandardPermission.ManageBrands,
            StandardPermission.ManageCollections,
            StandardPermission.ManageProductReviews,
            StandardPermission.ManageProductTags,
            StandardPermission.ManageProductAttributes,
            StandardPermission.ManageSpecificationAttributes,
            StandardPermission.ManageCheckoutAttribute,
            StandardPermission.ManageContactAttribute,
            StandardPermission.ManageCustomers,
            StandardPermission.ManageCustomerGroups,
            StandardPermission.ManageCustomerTags,
            StandardPermission.ManageSalesEmployees,
            StandardPermission.ManageAddressAttribute,
            StandardPermission.ManageCustomerAttribute,
            StandardPermission.ManageVendors,
            StandardPermission.ManageVendorReviews,
            StandardPermission.ManageCurrentCarts,
            StandardPermission.ManageOrders,
            StandardPermission.ManageShipments,
            StandardPermission.ManageGiftVouchers,
            StandardPermission.ManageMerchandiseReturns,
            StandardPermission.ManagePaymentTransactions,
            StandardPermission.ManageDocuments,
            StandardPermission.ManageReports,
            StandardPermission.ManageAffiliates,
            StandardPermission.ManagePushEvents,
            StandardPermission.ManageCampaigns,
            StandardPermission.ManageDiscounts,
            StandardPermission.ManageNewsletterSubscribers,
            StandardPermission.ManageNewsletterCategories,
            StandardPermission.ManageNews,
            StandardPermission.ManageBlog,
            StandardPermission.ManageWidgets,
            StandardPermission.ManagePages,
            StandardPermission.ManageKnowledgebase,
            StandardPermission.ManageCourses,
            StandardPermission.ManageMessageTemplates,
            StandardPermission.ManageCountries,
            StandardPermission.ManageLanguages,
            StandardPermission.ManageSettings,
            StandardPermission.ManagePaymentMethods,
            StandardPermission.ManageExternalAuthenticationMethods,
            StandardPermission.ManageTaxSettings,
            StandardPermission.ManageShippingSettings,
            StandardPermission.ManageCurrencies,
            StandardPermission.ManageMeasures,
            StandardPermission.ManageAcl,
            StandardPermission.ManageEmailAccounts,
            StandardPermission.ManageStores,
            StandardPermission.ManagePlugins,
            StandardPermission.ManageMessageQueue,
            StandardPermission.ManageMessageContactForm,
            StandardPermission.ManageMaintenance,
            StandardPermission.ManageSystem,
            StandardPermission.ManageFiles,
            StandardPermission.ManagePictures,
            StandardPermission.HtmlEditorManagePictures,
            StandardPermission.ManageScheduleTasks,
            StandardPermission.DisplayPrices,
            StandardPermission.EnableShoppingCart,
            StandardPermission.EnableWishlist,
            StandardPermission.PublicStoreAllowNavigation,
            StandardPermission.AccessClosedStore,
            StandardPermission.ManageOrderTags,
            StandardPermission.ManageOrderStatus,
            StandardPermission.AllowUseApi
        ];
    }

    public static IEnumerable<DefaultPermission> DefaultPermissions()
    {
        return [
            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.Administrators,
                Permissions = [
                    StandardPermission.ManageAccessAdminPanel,
                    StandardPermission.AllowCustomerImpersonation,
                    StandardPermission.ManageProducts,
                    StandardPermission.ManageCategories,
                    StandardPermission.ManageBrands,
                    StandardPermission.ManageCollections,
                    StandardPermission.ManageProductReviews,
                    StandardPermission.ManageProductTags,
                    StandardPermission.ManageOrderStatus,
                    StandardPermission.ManageOrderTags,
                    StandardPermission.ManageProductAttributes,
                    StandardPermission.ManageSpecificationAttributes,
                    StandardPermission.ManageCheckoutAttribute,
                    StandardPermission.ManageContactAttribute,
                    StandardPermission.ManageCustomers,
                    StandardPermission.ManageCustomerGroups,
                    StandardPermission.ManageCustomerTags,
                    StandardPermission.ManageVendors,
                    StandardPermission.ManageSalesEmployees,
                    StandardPermission.ManageVendorReviews,
                    StandardPermission.ManageAddressAttribute,
                    StandardPermission.ManageCustomerAttribute,
                    StandardPermission.ManageCurrentCarts,
                    StandardPermission.ManageOrders,
                    StandardPermission.ManageShipments,
                    StandardPermission.ManageGiftVouchers,
                    StandardPermission.ManagePaymentTransactions,
                    StandardPermission.ManageMerchandiseReturns,
                    StandardPermission.ManageDocuments,
                    StandardPermission.ManageReports,
                    StandardPermission.ManageAffiliates,
                    StandardPermission.ManagePushEvents,
                    StandardPermission.ManageCampaigns,
                    StandardPermission.ManageDiscounts,
                    StandardPermission.ManageNewsletterSubscribers,
                    StandardPermission.ManageNewsletterCategories,
                    StandardPermission.ManageNews,
                    StandardPermission.ManageBlog,
                    StandardPermission.ManageWidgets,
                    StandardPermission.ManagePages,
                    StandardPermission.ManageKnowledgebase,
                    StandardPermission.ManageCourses,
                    StandardPermission.ManageMessageTemplates,
                    StandardPermission.ManageCountries,
                    StandardPermission.ManageLanguages,
                    StandardPermission.ManageSettings,
                    StandardPermission.ManagePaymentMethods,
                    StandardPermission.ManageExternalAuthenticationMethods,
                    StandardPermission.ManageTaxSettings,
                    StandardPermission.ManageShippingSettings,
                    StandardPermission.ManageCurrencies,
                    StandardPermission.ManageMeasures,
                    StandardPermission.ManageAcl,
                    StandardPermission.ManageEmailAccounts,
                    StandardPermission.ManageStores,
                    StandardPermission.ManagePlugins,
                    StandardPermission.ManageMessageQueue,
                    StandardPermission.ManageMessageContactForm,
                    StandardPermission.ManageMaintenance,
                    StandardPermission.ManageSystem,
                    StandardPermission.ManageFiles,
                    StandardPermission.ManagePictures,
                    StandardPermission.HtmlEditorManagePictures,
                    StandardPermission.ManageScheduleTasks,
                    StandardPermission.DisplayPrices,
                    StandardPermission.EnableShoppingCart,
                    StandardPermission.EnableWishlist,
                    StandardPermission.PublicStoreAllowNavigation,
                    StandardPermission.AccessClosedStore,
                    StandardPermission.AllowUseApi
                ]
            },
            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.Guests,
                Permissions = [
                    StandardPermission.DisplayPrices,
                    StandardPermission.EnableShoppingCart,
                    StandardPermission.EnableWishlist,
                    StandardPermission.PublicStoreAllowNavigation
                ]
            },
            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.Registered,
                Permissions = [
                    StandardPermission.DisplayPrices,
                    StandardPermission.EnableShoppingCart,
                    StandardPermission.EnableWishlist,
                    StandardPermission.PublicStoreAllowNavigation,
                    StandardPermission.AllowUseApi
                ]
            },
            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.Vendors,
                Permissions = [
                    StandardPermission.ManageAccessVendorPanel,
                    StandardPermission.ManageProducts,
                    StandardPermission.ManageFiles,
                    StandardPermission.ManagePictures,
                    StandardPermission.ManageOrders,
                    StandardPermission.ManageVendorReviews,
                    StandardPermission.ManageShipments,
                    StandardPermission.ManageMerchandiseReturns,
                    StandardPermission.ManageReports
                ]
            },
            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.Staff,
                Permissions = [
                    StandardPermission.ManageAccessAdminPanel,
                    StandardPermission.ManageProducts,
                    StandardPermission.ManageFiles,
                    StandardPermission.ManagePictures,
                    StandardPermission.ManageCategories,
                    StandardPermission.ManageBrands,
                    StandardPermission.ManageCollections,
                    StandardPermission.ManageOrders,
                    StandardPermission.ManagePaymentTransactions,
                    StandardPermission.ManageShipments,
                    StandardPermission.ManageMerchandiseReturns,
                    StandardPermission.ManageReports
                ]
            },

            new DefaultPermission {
                CustomerGroupSystemName = SystemCustomerGroupNames.SalesManager,
                Permissions = [
                    StandardPermission.ManageAccessAdminPanel,
                    StandardPermission.ManageOrders,
                    StandardPermission.ManageCustomers
                ]
            }
        ];
    }
}
