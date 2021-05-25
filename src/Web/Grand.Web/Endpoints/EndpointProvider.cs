using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Endpoints
{
    public partial class EndpointProvider : IEndpointProvider
    {
        public void RegisterEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
        {
            var pattern = "";
            if (DataSettingsManager.DatabaseIsInstalled())
            {
                var config = endpointRouteBuilder.ServiceProvider.GetRequiredService<AppConfig>();
                if (config.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    pattern = $"{{language:lang={config.SeoFriendlyUrlsDefaultCode}}}/";
                }
            }

            //home page
            endpointRouteBuilder.MapControllerRoute("HomePage", pattern, new { controller = "Home", action = "Index" });


            RegisterAccountRoute(endpointRouteBuilder, pattern);

            RegisterVendorRoute(endpointRouteBuilder, pattern);

            RegisterCartRoute(endpointRouteBuilder, pattern);

            RegisterOrderRoute(endpointRouteBuilder, pattern);

            RegisterMerchandiseReturnRoute(endpointRouteBuilder, pattern);

            RegisterCommonRoute(endpointRouteBuilder, pattern);

            RegisterCatalogRoute(endpointRouteBuilder, pattern);

            RegisterProductRoute(endpointRouteBuilder, pattern);

            RegisterCmsRoute(endpointRouteBuilder, pattern);

            RegisterBlogRoute(endpointRouteBuilder, pattern);

            RegisterNewsletterRoute(endpointRouteBuilder, pattern);

            RegisterAddToCartRoute(endpointRouteBuilder, pattern);

            RegisterOutOfStockSubscriptionRoute(endpointRouteBuilder, pattern);

            RegisterCheckoutRoute(endpointRouteBuilder, pattern);

            RegisterDownloadRoute(endpointRouteBuilder, pattern);

            RegisterPageRoute(endpointRouteBuilder, pattern);

            RegisterInstallRoute(endpointRouteBuilder, pattern);

        }

        public int Priority => 0;

        private void RegisterAccountRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //login
            endpointRouteBuilder.MapControllerRoute("Login",
                            $"{pattern}login/",
                            new { controller = "Account", action = "Login" });

            // two factor authorization digit code page
            endpointRouteBuilder.MapControllerRoute("TwoFactorAuthorization",
                            $"{pattern}two-factor-authorization/",
                            new { controller = "Account", action = "TwoFactorAuthorization" });

            //register
            endpointRouteBuilder.MapControllerRoute("Register",
                            $"{pattern}register/",
                            new { controller = "Account", action = "Register" });
            //logout
            endpointRouteBuilder.MapControllerRoute("Logout",
                            $"{pattern}logout/",
                            new { controller = "Account", action = "Logout" });

            //customer account links
            endpointRouteBuilder.MapControllerRoute("CustomerInfo",
                            pattern + "account/info",
                            new { controller = "Account", action = "Info" });

            // enable two factor authorization digit code page
            endpointRouteBuilder.MapControllerRoute("EnableTwoFactorAuthorization",
                           "account/enable-two-factor-authorization",
                           new { controller = "Account", action = "EnableTwoFactorAuthenticator" });

            endpointRouteBuilder.MapControllerRoute("CustomerAddresses",
                            pattern + "account/addresses",
                            new { controller = "Account", action = "Addresses" });

            //login page for checkout as guest
            endpointRouteBuilder.MapControllerRoute("LoginCheckoutAsGuest",
                            pattern + "login/checkoutasguest",
                            new { controller = "Account", action = "Login", checkoutAsGuest = true });

            //register result page
            endpointRouteBuilder.MapControllerRoute("RegisterResult",
                            pattern + "registerresult/{resultId}",
                            new { controller = "Account", action = "RegisterResult" });

            //check username availability
            endpointRouteBuilder.MapControllerRoute("CheckUsernameAvailability",
                            pattern + "account/checkusernameavailability",
                            new { controller = "Account", action = "CheckUsernameAvailability" });

            //passwordrecovery
            endpointRouteBuilder.MapControllerRoute("PasswordRecovery",
                            pattern + "passwordrecovery",
                            new { controller = "Account", action = "PasswordRecovery" });

            //password recovery confirmation
            endpointRouteBuilder.MapControllerRoute("PasswordRecoveryConfirm",
                            pattern + "passwordrecovery/confirm",
                            new { controller = "Account", action = "PasswordRecoveryConfirm" });

            endpointRouteBuilder.MapControllerRoute("CustomerAuctions",
                            pattern + "account/auctions",
                            new { controller = "Account", action = "Auctions" });

            endpointRouteBuilder.MapControllerRoute("CustomerNotes",
                            pattern + "account/notes",
                            new { controller = "Account", action = "Notes" });

            endpointRouteBuilder.MapControllerRoute("CustomerDocuments",
                            pattern + "account/documents",
                            new { controller = "Account", action = "Documents" });

            endpointRouteBuilder.MapControllerRoute("CustomerCourses",
                            pattern + "account/courses",
                            new { controller = "Account", action = "Courses" });

            endpointRouteBuilder.MapControllerRoute("AccountActivation",
                            pattern + "account/activation",
                            new { controller = "Account", action = "AccountActivation" });

            endpointRouteBuilder.MapControllerRoute("CustomerReviews",
                            pattern + "account/reviews",
                            new { controller = "Account", action = "Reviews" });

            endpointRouteBuilder.MapControllerRoute("CustomerSubAccounts",
                            pattern + "account/subaccounts",
                            new { controller = "Account", action = "SubAccounts" });

            endpointRouteBuilder.MapControllerRoute("CustomerSubAccountAdd",
                            pattern + "account/subaccountadd",
                            new { controller = "Account", action = "SubAccountAdd" });

            endpointRouteBuilder.MapControllerRoute("CustomerSubAccountEdit",
                            pattern + "account/subaccountedit",
                            new { controller = "Account", action = "SubAccountEdit" });

            endpointRouteBuilder.MapControllerRoute("CustomerSubAccountDelete",
                            pattern + "account/subaccountdelete",
                            new { controller = "Account", action = "SubAccountDelete" });

            endpointRouteBuilder.MapControllerRoute("CustomerDownloadableProducts",
                            pattern + "account/downloadableproducts",
                            new { controller = "Account", action = "DownloadableProducts" });

            endpointRouteBuilder.MapControllerRoute("CustomerChangePassword",
                            pattern + "account/changepassword",
                            new { controller = "Account", action = "ChangePassword" });

            endpointRouteBuilder.MapControllerRoute("CustomerDeleteAccount",
                            pattern + "account/deleteaccount",
                            new { controller = "Account", action = "DeleteAccount" });

            endpointRouteBuilder.MapControllerRoute("CustomerAddressEdit",
                            pattern + "account/addressedit/{addressId}",
                            new { controller = "Account", action = "AddressEdit" });

            endpointRouteBuilder.MapControllerRoute("CustomerAddressAdd",
                            pattern + "account/addressadd",
                            new { controller = "Account", action = "AddressAdd" });

            endpointRouteBuilder.MapControllerRoute("DownloadUserAgreement",
                            pattern + "account/useragreement/{orderItemId}",
                            new { controller = "Account", action = "UserAgreement" });


        }
        private void RegisterVendorRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //vendor info
            endpointRouteBuilder.MapControllerRoute("CustomerVendorInfo",
                            pattern + "vendor/vendorinfo",
                            new { controller = "Vendor", action = "Info" });

            //apply for vendor account
            endpointRouteBuilder.MapControllerRoute("ApplyVendorAccount",
                            pattern + "vendor/apply",
                            new { controller = "Vendor", action = "ApplyVendor" });

            //contact vendor
            endpointRouteBuilder.MapControllerRoute("ContactVendor",
                            pattern + "vendor/contact/{vendorId}",
                            new { controller = "Vendor", action = "ContactVendor" });

        }

        private void RegisterCatalogRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //product search
            endpointRouteBuilder.MapControllerRoute("ProductSearch",
                            pattern + "search/",
                            new { controller = "Catalog", action = "Search" });

            endpointRouteBuilder.MapControllerRoute("ProductSearchAutoComplete",
                            pattern + "catalog/searchtermautocomplete",
                            new { controller = "Catalog", action = "SearchTermAutoComplete" });

            //product tags
            endpointRouteBuilder.MapControllerRoute("ProductTagsAll",
                            pattern + "producttag/all/",
                            new { controller = "Catalog", action = "ProductTagsAll" });

            //brands
            endpointRouteBuilder.MapControllerRoute("BrandList",
                            pattern + "brand/all/",
                            new { controller = "Catalog", action = "BrandAll" });

            //collections
            endpointRouteBuilder.MapControllerRoute("CollectionList",
                            pattern + "collection/all/",
                            new { controller = "Catalog", action = "CollectionAll" });

            //vendors
            endpointRouteBuilder.MapControllerRoute("VendorList",
                            pattern + "vendor/all/",
                            new { controller = "Catalog", action = "VendorAll" });

            //product tags
            endpointRouteBuilder.MapControllerRoute("ProductsByTag",
                            pattern + "producttag/{productTagId}/{SeName}",
                            new { controller = "Catalog", action = "ProductsByTag" });

            endpointRouteBuilder.MapControllerRoute("ProductsByTagName",
                            pattern + "producttag/{SeName}",
                            new { controller = "Catalog", action = "ProductsByTagName" });

            //vendor reviews
            endpointRouteBuilder.MapControllerRoute("VendorReviews",
                            pattern + "vendoreviews/{vendorId}",
                            new { controller = "Catalog", action = "VendorReviews" });

            //set review helpfulness (AJAX link)
            endpointRouteBuilder.MapControllerRoute("SetVendorReviewHelpfulness",
                            pattern + "setvendorreviewhelpfulness",
                            new { controller = "Catalog", action = "SetVendorReviewHelpfulness" });

        }

        private void RegisterProductRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //recently viewed products
            endpointRouteBuilder.MapControllerRoute("RecentlyViewedProducts",
                            pattern + "recentlyviewedproducts/",
                            new { controller = "Product", action = "RecentlyViewedProducts" });

            //new products
            endpointRouteBuilder.MapControllerRoute("NewProducts",
                            pattern + "newproducts/",
                            new { controller = "Product", action = "NewProducts" });

            //compare products
            endpointRouteBuilder.MapControllerRoute("CompareProducts",
                            pattern + "compareproducts/",
                            new { controller = "Product", action = "CompareProducts" });

            //quick view product
            endpointRouteBuilder.MapControllerRoute("QuickView-Product",
                            pattern + "quickview/product/{productId?}",
                            new { controller = "Product", action = "QuickView" },
                            new { productId = @"\w+" },
                            new[] { "Grand.Web.Controllers" });

            //product email a friend
            endpointRouteBuilder.MapControllerRoute("ProductEmailAFriend",
                            pattern + "productemailafriend/{productId}",
                            new { controller = "Product", action = "ProductEmailAFriend" });

            //product ask question
            endpointRouteBuilder.MapControllerRoute("AskQuestion",
                            pattern + "askquestion/{productId}",
                            new { controller = "Product", action = "AskQuestion" });

            //product ask question on product page
            endpointRouteBuilder.MapControllerRoute("AskQuestionOnProduct",
                            pattern + "askquestiononproduct",
                            new { controller = "Product", action = "AskQuestionOnProduct" });

            //reviews
            endpointRouteBuilder.MapControllerRoute("ProductReviews",
                            pattern + "productreviews/{productId}",
                            new { controller = "Product", action = "ProductReviews" });

            //comparing products
            endpointRouteBuilder.MapControllerRoute("AddProductToCompare",
                            pattern + "compareproducts/add/{productId?}",
                            new { controller = "Product", action = "AddProductToCompareList" });

            //set review helpfulness (AJAX link)
            endpointRouteBuilder.MapControllerRoute("SetProductReviewHelpfulness",
                            pattern + "setproductreviewhelpfulness",
                            new { controller = "Product", action = "SetProductReviewHelpfulness" });

            //comparing products
            endpointRouteBuilder.MapControllerRoute("RemoveProductFromCompareList",
                            pattern + "compareproducts/remove/{productId}",
                            new { controller = "Product", action = "RemoveProductFromCompareList" });

            endpointRouteBuilder.MapControllerRoute("ClearCompareList",
                            pattern + "clearcomparelist/",
                            new { controller = "Product", action = "ClearCompareList" });


            //product attributes with "upload file" type
            endpointRouteBuilder.MapControllerRoute("UploadFileProductAttribute",
                            pattern + "uploadfileproductattribute/{attributeId}",
                            new { controller = "Product", action = "UploadFileProductAttribute" });
        }

        private void RegisterCommonRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {

            //contact us
            endpointRouteBuilder.MapControllerRoute("ContactUs",
                            pattern + "contactus",
                            new { controller = "Common", action = "ContactUs" });

            //interactive form
            endpointRouteBuilder.MapControllerRoute("PopupInteractiveForm",
                            pattern + "popupinteractiveform",
                            new { controller = "Common", action = "PopupInteractiveForm" });

            //change currency 
            endpointRouteBuilder.MapControllerRoute("ChangeCurrency",
                            pattern + "changecurrency/{customercurrency}",
                            new { controller = "Common", action = "SetCurrency" });

            //change language 
            endpointRouteBuilder.MapControllerRoute("ChangeLanguage",
                            pattern + "changelanguage/{langid}",
                            new { controller = "Common", action = "SetLanguage" });

            //change tax 
            endpointRouteBuilder.MapControllerRoute("ChangeTaxType",
                            pattern + "changetaxtype/{customertaxtype}",
                            new { controller = "Common", action = "SetTaxType" });

            //change store 
            endpointRouteBuilder.MapControllerRoute("ChangeStore",
                            pattern + "changestore/{store}",
                            new { controller = "Common", action = "SetStore" });

            //get state list by country ID 
            endpointRouteBuilder.MapControllerRoute("GetStatesByCountryId",
                            pattern + "country/getstatesbycountryid/",
                            new { controller = "Country", action = "GetStatesByCountryId" });

            //Cookie accept button handler 
            endpointRouteBuilder.MapControllerRoute("CookieAccept",
                            pattern + "cookieaccept",
                            new { controller = "Common", action = "CookieAccept" });

            //Privacy Preference settings
            endpointRouteBuilder.MapControllerRoute("PrivacyPreference",
                pattern + "privacypreference",
                new { controller = "Common", action = "PrivacyPreference" });

            // contact attributes with "upload file" type
            endpointRouteBuilder.MapControllerRoute("UploadFileContactAttribute",
                            pattern + "uploadfilecontactattribute/{attributeId}",
                            new { controller = "Common", action = "UploadFileContactAttribute" });

            //CurrentPosition Save
            endpointRouteBuilder.MapControllerRoute("CurrentPosition",
                pattern + "currentposition",
                new { controller = "Common", action = "SaveCurrentPosition" });

            //robots.txt
            endpointRouteBuilder.MapControllerRoute("robots.txt",
                            pattern + "robots.txt",
                            new { controller = "Common", action = "RobotsTextFile" });

            //sitemap
            endpointRouteBuilder.MapControllerRoute("Sitemap",
                            pattern + "sitemap",
                            new { controller = "Common", action = "Sitemap" });

            //store closed
            endpointRouteBuilder.MapControllerRoute("StoreClosed",
                            pattern + "storeclosed",
                            new { controller = "Common", action = "StoreClosed" });

            //page not found
            endpointRouteBuilder.MapControllerRoute("PageNotFound",
                            pattern + "page-not-found",
                            new { controller = "Common", action = "PageNotFound" });

            //access denied
            endpointRouteBuilder.MapControllerRoute("AccessDenied",
                            pattern + "access-denied",
                            new { controller = "Common", action = "AccessDenied" });

            //lets encrypt
            endpointRouteBuilder.MapControllerRoute("well-known",
                            ".well-known/pki-validation/{fileName}",
                            new { controller = "LetsEncrypt", action = "Index" });


        }
        private void RegisterCheckoutRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //checkout pages
            endpointRouteBuilder.MapControllerRoute("Checkout",
                            pattern + "checkout/",
                            new { controller = "Checkout", action = "Start" });

            endpointRouteBuilder.MapControllerRoute("CheckoutCompleted",
                            pattern + "checkout/completed/{orderId?}",
                            new { controller = "Checkout", action = "Completed" });
        }

        private void RegisterAddToCartRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {

            //add product to cart (without any attributes and options). used on catalog pages.
            endpointRouteBuilder.MapControllerRoute("AddProductCatalog",
                            pattern + "addproducttocart/catalog/{productId?}/{shoppingCartTypeId?}",
                            new { controller = "ActionCart", action = "AddProductCatalog" },
                            new { productId = @"\w+", shoppingCartTypeId = @"\d+" },
                            new[] { "Grand.Web.Controllers" });

            //add product to cart (with attributes and options). used on the product details pages.
            endpointRouteBuilder.MapControllerRoute("AddProductDetails",
                            pattern + "addproducttocart/details/{productId?}/{shoppingCartTypeId?}",
                            new { controller = "ActionCart", action = "AddProductDetails" },
                            new { productId = @"\w+", shoppingCartTypeId = @"\d+" },
                            new[] { "Grand.Web.Controllers" });

            //add product to bid, use on the product details page
            endpointRouteBuilder.MapControllerRoute("AddBid",
                            pattern + "addbid/AddBid/{productId?}/{shoppingCartTypeId?}",
                            new { controller = "ActionCart", action = "AddBid" },
                            new { productId = @"\w+", shoppingCartTypeId = @"\d+" },
                            new[] { "Grand.Web.Controllers" });

        }

        private void RegisterCmsRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {

            //widgets
            endpointRouteBuilder.MapControllerRoute("WidgetsByZone",
                            $"{pattern}widgetsbyzone/",
                            new { controller = "Widget", action = "WidgetsByZone" });

            //knowledgebase
            endpointRouteBuilder.MapControllerRoute("Knowledgebase",
                            pattern + "knowledgebase",
                            new { controller = "Knowledgebase", action = "List" });

            endpointRouteBuilder.MapControllerRoute("KnowledgebaseSearch",
                            pattern + "knowledgebase/itemsbykeyword/{keyword?}",
                            new { controller = "Knowledgebase", action = "ItemsByKeyword" });

            //news
            endpointRouteBuilder.MapControllerRoute("NewsArchive",
                            pattern + "news",
                            new { controller = "News", action = "List" });

            //pixel
            endpointRouteBuilder.MapControllerRoute("PixelQueuedEmail",
                           "queuedemail/pixel.png",
                           new { controller = "Pixel", action = "QueuedEmail" });
        }

        private void RegisterBlogRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //blog
            endpointRouteBuilder.MapControllerRoute("Blog",
                            pattern + "blog",
                            new { controller = "Blog", action = "List" });

            //blog
            endpointRouteBuilder.MapControllerRoute("BlogByTag",
                            pattern + "blog/tag/{tag}",
                            new { controller = "Blog", action = "BlogByTag" });

            endpointRouteBuilder.MapControllerRoute("BlogByMonth",
                            pattern + "blog/month/{month}",
                            new { controller = "Blog", action = "BlogByMonth" });

            endpointRouteBuilder.MapControllerRoute("BlogByCategory",
                            pattern + "blog/category/{categorySeName}",
                            new { controller = "Blog", action = "BlogByCategory" });

            endpointRouteBuilder.MapControllerRoute("BlogByKeyword",
                            pattern + "blog/keyword/{searchKeyword?}",
                            new { controller = "Blog", action = "BlogByKeyword" });

        }
        private void RegisterNewsletterRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //assign newsletters to categories
            endpointRouteBuilder.MapControllerRoute("SubscribeNewsletterCategory",
                            pattern + "newsletter/savecategories",
                            new { controller = "Newsletter", action = "SaveCategories" });

            //subscribe newsletters
            endpointRouteBuilder.MapControllerRoute("SubscribeNewsletter",
                            pattern + "subscribenewsletter",
                            new { controller = "Newsletter", action = "SubscribeNewsletter" });

            //activate newsletters
            endpointRouteBuilder.MapControllerRoute("NewsletterActivation",
                            pattern + "newsletter/subscriptionactivation/{token}/{active}",
                            new { controller = "Newsletter", action = "SubscriptionActivation" });

        }
        private void RegisterCartRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //shopping cart
            endpointRouteBuilder.MapControllerRoute("ShoppingCart",
                            $"{pattern}cart/",
                            new { controller = "ShoppingCart", action = "Cart" });

            //Continue shopping
            endpointRouteBuilder.MapControllerRoute("ContinueShopping",
                            $"{pattern}cart/continueshopping/",
                            new { controller = "ShoppingCart", action = "ContinueShopping" });

            //clear cart
            endpointRouteBuilder.MapControllerRoute("ClearCart",
                            $"{pattern}cart/clear/",
                            new { controller = "ShoppingCart", action = "ClearCart" });

            //start checkout
            endpointRouteBuilder.MapControllerRoute("StartCheckout",
                            $"{pattern}cart/checkout/",
                            new { controller = "ShoppingCart", action = "StartCheckout" });

            endpointRouteBuilder.MapControllerRoute("ApplyDiscountCoupon",
                            $"{pattern}applydiscountcoupon/",
                            new { controller = "ShoppingCart", action = "ApplyDiscountCoupon" });

            endpointRouteBuilder.MapControllerRoute("RemoveDiscountCoupon",
                            $"{pattern}removediscountcoupon/",
                            new { controller = "ShoppingCart", action = "RemoveDiscountCoupon" });

            endpointRouteBuilder.MapControllerRoute("ApplyGiftVoucher",
                            $"{pattern}applygiftvoucher/",
                            new { controller = "ShoppingCart", action = "ApplyGiftVoucher" });

            endpointRouteBuilder.MapControllerRoute("RemoveGiftVoucherCode",
                            $"{pattern}removegiftvouchercode/",
                            new { controller = "ShoppingCart", action = "RemoveGiftVoucherCode" });

            endpointRouteBuilder.MapControllerRoute("UpdateCart",
                            $"{pattern}updatecart/",
                            new { controller = "ShoppingCart", action = "UpdateCart" });

            //get state list by country ID  (AJAX link)
            endpointRouteBuilder.MapControllerRoute("DeleteCartItem",
                            pattern + "deletecartitem/{id?}",
                            new { controller = "ShoppingCart", action = "DeleteCartItem" });

            endpointRouteBuilder.MapControllerRoute("ChangeTypeCartItem",
                            pattern + "changetypecartitem/{id?}",
                            new { controller = "ShoppingCart", action = "ChangeTypeCartItem" });

            //estimate shipping
            endpointRouteBuilder.MapControllerRoute("EstimateShipping",
                            $"{pattern}cart/estimateshipping",
                            new { controller = "ShoppingCart", action = "GetEstimateShipping" });

            //checkout attributes with "upload file" type
            endpointRouteBuilder.MapControllerRoute("UploadFileCheckoutAttribute",
                            pattern + "uploadfilecheckoutattribute/{attributeId}",
                            new { controller = "ShoppingCart", action = "UploadFileCheckoutAttribute" });

            //wishlist
            endpointRouteBuilder.MapControllerRoute("Wishlist",
                            pattern + "wishlist/{customerGuid?}",
                            new { controller = "Wishlist", action = "Index" });

            //email wishlist
            endpointRouteBuilder.MapControllerRoute("EmailWishlist",
                            pattern + "emailwishlist",
                            new { controller = "Wishlist", action = "EmailWishlist" });

            //email wishlist
            endpointRouteBuilder.MapControllerRoute("UpdateWishlist",
                            pattern + "updatewishlist",
                            new { controller = "Wishlist", action = "UpdateWishlist" });
            //email wishlist
            endpointRouteBuilder.MapControllerRoute("AddItemsToCartFromWishlist",
                            pattern + "additemstocartwishlist",
                            new { controller = "Wishlist", action = "AddItemsToCartFromWishlist" });
        }

        private void RegisterOrderRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            endpointRouteBuilder.MapControllerRoute("CustomerOrders",
                            pattern + "order/history",
                            new { controller = "Order", action = "CustomerOrders" });

            endpointRouteBuilder.MapControllerRoute("CustomerLoyaltyPoints",
                            pattern + "loyaltypoints/history",
                            new { controller = "Order", action = "CustomerLoyaltyPoints" });

            //orders
            endpointRouteBuilder.MapControllerRoute("OrderDetails",
                            pattern + "orderdetails/{orderId}",
                            new { controller = "Order", action = "Details" });

            endpointRouteBuilder.MapControllerRoute("ShipmentDetails",
                            pattern + "orderdetails/shipment/{shipmentId}",
                            new { controller = "Order", action = "ShipmentDetails" });


            endpointRouteBuilder.MapControllerRoute("ReOrder",
                           pattern + "reorder/{orderId}",
                           new { controller = "Order", action = "ReOrder" });

            endpointRouteBuilder.MapControllerRoute("GetOrderPdfInvoice",
                            pattern + "orderdetails/pdf/{orderId}",
                            new { controller = "Order", action = "GetPdfInvoice" });

            endpointRouteBuilder.MapControllerRoute("PrintOrderDetails",
                            pattern + "orderdetails/print/{orderId}",
                            new { controller = "Order", action = "PrintOrderDetails" });

            endpointRouteBuilder.MapControllerRoute("CancelOrder",
                            pattern + "orderdetails/cancel/{orderId}",
                            new { controller = "Order", action = "CancelOrder" });

            endpointRouteBuilder.MapControllerRoute("AddOrderNote",
                            "orderdetails/ordernote/{orderId}",
                            new { controller = "Order", action = "AddOrderNote" });

        }

        private void RegisterOutOfStockSubscriptionRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //out of stock notifications
            endpointRouteBuilder.MapControllerRoute("OutOfStockSubscribePopup",
                            pattern + "outofstocksubscribe/{productId}",
                            new { controller = "OutOfStockSubscription", action = "SubscribePopup" });

            //out of stock notifications button text
            endpointRouteBuilder.MapControllerRoute("OutOfStockSubscribeButton",
                            pattern + "outofstocksubscribebutton/{productId}",
                            new { controller = "OutOfStockSubscription", action = "SubscribeButton" });

            endpointRouteBuilder.MapControllerRoute("CustomerOutOfStockSubscriptions",
                           pattern + "outofstocksubscriptions/manage",
                           new { controller = "OutOfStockSubscription", action = "CustomerSubscriptions" });

            endpointRouteBuilder.MapControllerRoute("CustomerOutOfStockSubscriptionsPaged",
                            pattern + "outofstocksubscriptions/manage/{pageNumber}",
                            new { controller = "OutOfStockSubscription", action = "CustomerSubscriptions" });
        }

        private void RegisterMerchandiseReturnRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //customer account links
            endpointRouteBuilder.MapControllerRoute("CustomerMerchandiseReturns",
                            pattern + "merchandisereturn/history",
                            new { controller = "MerchandiseReturn", action = "CustomerMerchandiseReturns" });

            endpointRouteBuilder.MapControllerRoute("MerchandiseReturn",
                            pattern + "merchandisereturn/{orderId}",
                            new { controller = "MerchandiseReturn", action = "MerchandiseReturn" });

            endpointRouteBuilder.MapControllerRoute("MerchandiseReturnDetails",
                            pattern + "merchandisereturndetails/{merchandiseReturnId}",
                            new { controller = "MerchandiseReturn", action = "MerchandiseReturnDetails" });
        }

        private void RegisterDownloadRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //downloads
            endpointRouteBuilder.MapControllerRoute("GetSampleDownload",
                            pattern + "download/sample/{productid}",
                            new { controller = "Download", action = "Sample" });

            //order downloads
            endpointRouteBuilder.MapControllerRoute("GetDownload",
                            pattern + "download/getdownload/{orderItemId}/{agree?}",
                            new { controller = "Download", action = "GetDownload" });

            endpointRouteBuilder.MapControllerRoute("GetLicense",
                            pattern + "download/getlicense/{orderItemId}/",
                            new { controller = "Download", action = "GetLicense" });


            endpointRouteBuilder.MapControllerRoute("GetOrderNoteFile",
                            pattern + "download/ordernotefile/{ordernoteid}",
                            new { controller = "Download", action = "GetOrderNoteFile" });

            endpointRouteBuilder.MapControllerRoute("GetShipmentNoteFile",
                            pattern + "download/shipmentnotefile/{shipmentnoteid}",
                            new { controller = "Download", action = "GetShipmentNoteFile" });

            endpointRouteBuilder.MapControllerRoute("GetCustomerNoteFile",
                            pattern + "download/customernotefile/{customernoteid}",
                            new { controller = "Download", action = "GetCustomerNoteFile" });

            endpointRouteBuilder.MapControllerRoute("GetMerchandiseReturnNoteFile",
                            pattern + "download/merchandisereturnnotefile/{merchandisereturnnoteid}",
                            new { controller = "Download", action = "GetMerchandiseReturnNoteFile" });

            endpointRouteBuilder.MapControllerRoute("GetDocumentFile",
                            pattern + "download/documentfile/{documentid}",
                            new { controller = "Download", action = "GetDocumentFile" });

        }
        private void RegisterPageRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //pages
            endpointRouteBuilder.MapControllerRoute("PagePopup",
                            pattern + "t-popup/{SystemName}",
                            new { controller = "Page", action = "PageDetailsPopup" });

            //authenticate page AJAX link
            endpointRouteBuilder.MapControllerRoute("PageAuthenticate",
                            pattern + "page/authenticate",
                            new { controller = "Page", action = "Authenticate" });
        }

        private void RegisterInstallRoute(IEndpointRouteBuilder endpointRouteBuilder, string pattern)
        {
            //install
            endpointRouteBuilder.MapControllerRoute("Installation", "install",
                            new { controller = "Install", action = "Index" });

            endpointRouteBuilder.MapControllerRoute("InstallChangeLanguage", "installchangelanguage",
                            new { controller = "Install", action = "ChangeLanguage" });
            //upgrade
            endpointRouteBuilder.MapControllerRoute("Upgrade", "upgrade",
                            new { controller = "Upgrade", action = "Index" });
        }
    }
}
