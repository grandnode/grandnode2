using Grand.Business.Common.Interfaces.Security;
using Grand.Domain.Customers;
using Grand.Domain.Permissions;
using System.Collections.Generic;

namespace Grand.Business.Common.Services.Security
{
    public class PermissionProvider : IPermissionProvider
    {
        public virtual IEnumerable<Permission> GetPermissions()
        {
            return new[]
            {
                StandardPermission.AccessAdminPanel,
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
                StandardPermission.ManageActions,
                StandardPermission.ManageReminders,
                StandardPermission.ManageBanners,
                StandardPermission.ManageInteractiveForm,
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
                StandardPermission.ManageActivityLog,
                StandardPermission.ManageAcl,
                StandardPermission.ManageEmailAccounts,
                StandardPermission.ManageStores,
                StandardPermission.ManagePlugins,
                StandardPermission.ManageSystemLog,
                StandardPermission.ManageMessageQueue,
                StandardPermission.ManageMessageContactForm,
                StandardPermission.ManageMaintenance,
                StandardPermission.ManageFiles,
                StandardPermission.ManagePictures,
                StandardPermission.ManageUserFields,
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
            };
        }

        public virtual IEnumerable<DefaultPermission> GetDefaultPermissions()
        {
            return new[]
            {
                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.Administrators,
                    Permissions = new[]
                    {
                        StandardPermission.AccessAdminPanel,
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
                        StandardPermission.ManageActivityLog,
                        StandardPermission.ManageAcl,
                        StandardPermission.ManageEmailAccounts,
                        StandardPermission.ManageStores,
                        StandardPermission.ManagePlugins,
                        StandardPermission.ManageSystemLog,
                        StandardPermission.ManageMessageQueue,
                        StandardPermission.ManageMessageContactForm,
                        StandardPermission.ManageMaintenance,
                        StandardPermission.ManageFiles,
                        StandardPermission.ManagePictures,
                        StandardPermission.ManageUserFields,
                        StandardPermission.HtmlEditorManagePictures,
                        StandardPermission.ManageScheduleTasks,
                        StandardPermission.DisplayPrices,
                        StandardPermission.EnableShoppingCart,
                        StandardPermission.EnableWishlist,
                        StandardPermission.PublicStoreAllowNavigation,
                        StandardPermission.AccessClosedStore,
                        StandardPermission.ManageBanners,
                        StandardPermission.ManageInteractiveForm,
                        StandardPermission.ManageActions,
                        StandardPermission.ManageReminders,
                        StandardPermission.AllowUseApi
                    }
                },
                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.Guests,
                    Permissions = new[]
                    {
                        StandardPermission.DisplayPrices,
                        StandardPermission.EnableShoppingCart,
                        StandardPermission.EnableWishlist,
                        StandardPermission.PublicStoreAllowNavigation
                    }
                },
                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.Registered,
                    Permissions = new[]
                    {
                        StandardPermission.DisplayPrices,
                        StandardPermission.EnableShoppingCart,
                        StandardPermission.EnableWishlist,
                        StandardPermission.PublicStoreAllowNavigation,
                        StandardPermission.AllowUseApi
                    }
                },
                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.Vendors,
                    Permissions = new[]
                    {
                        StandardPermission.AccessAdminPanel,
                        StandardPermission.ManageProducts,
                        StandardPermission.ManageFiles,
                        StandardPermission.ManagePictures,
                        StandardPermission.ManageOrders,
                        StandardPermission.ManageVendorReviews,
                        StandardPermission.ManageShipments
                    }
                },
                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.Staff,
                    Permissions = new[]
                    {
                        StandardPermission.AccessAdminPanel,
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
                    }
                },

                new DefaultPermission
                {
                    CustomerGroupSystemName = SystemCustomerGroupNames.SalesManager,
                    Permissions = new[]
                    {
                        StandardPermission.AccessAdminPanel,
                        StandardPermission.ManageOrders,
                        StandardPermission.ManageCustomers
                    }
                }
            };
        }
    }
}
