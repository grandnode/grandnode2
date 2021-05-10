using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallActivityLogTypes()
        {
            var activityLogTypes = new List<ActivityLogType>
                                      {
                                          //admin area activities
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewCategory",
                                                  Enabled = true,
                                                  Name = "Add a new category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewCheckoutAttribute",
                                                  Enabled = true,
                                                  Name = "Add a new checkout attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewContactAttribute",
                                                  Enabled = true,
                                                  Name = "Add a new contact attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewCustomer",
                                                  Enabled = true,
                                                  Name = "Add a new customer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewCustomerGroup",
                                                  Enabled = true,
                                                  Name = "Add a new customer group"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewDiscount",
                                                  Enabled = true,
                                                  Name = "Add a new discount"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewDocument",
                                                  Enabled = false,
                                                  Name = "Add a new document"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewDocumentType",
                                                  Enabled = false,
                                                  Name = "Add a new document type"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewGiftVoucher",
                                                  Enabled = true,
                                                  Name = "Add a new gift voucher"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewCollection",
                                                  Enabled = true,
                                                  Name = "Add a new collection"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewProduct",
                                                  Enabled = true,
                                                  Name = "Add a new product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewProductAttribute",
                                                  Enabled = true,
                                                  Name = "Add a new product attribute"
                                              },
                                           new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewSalesEmployee",
                                                  Enabled = true,
                                                  Name = "Add a sales employee"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewSetting",
                                                  Enabled = true,
                                                  Name = "Add a new setting"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewSpecAttribute",
                                                  Enabled = true,
                                                  Name = "Add a new specification attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                SystemKeyword = "AddNewPage",
                                                Enabled = true,
                                                Name = "Add a new page"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddNewWidget",
                                                  Enabled = true,
                                                  Name = "Add a new widget"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "AddLoyaltyPoints",
                                                  Enabled = true,
                                                  Name = "Assign new loyalty points"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteBid",
                                                  Enabled = true,
                                                  Name = "Delete bid"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteCategory",
                                                  Enabled = true,
                                                  Name = "Delete category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteCheckoutAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a checkout attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteContactAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a contact attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteCustomer",
                                                  Enabled = true,
                                                  Name = "Delete a customer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteCustomerGroup",
                                                  Enabled = true,
                                                  Name = "Delete a customer group"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteDiscount",
                                                  Enabled = true,
                                                  Name = "Delete a discount"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteDocument",
                                                  Enabled = false,
                                                  Name = "Delete document"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteDocumentType",
                                                  Enabled = false,
                                                  Name = "Delete document type"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteGiftVoucher",
                                                  Enabled = true,
                                                  Name = "Delete a gift voucher"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteCollection",
                                                  Enabled = true,
                                                  Name = "Delete a collection"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteOrder",
                                                  Enabled = true,
                                                  Name = "Delete an order"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteProduct",
                                                  Enabled = true,
                                                  Name = "Delete a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteProductAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a product attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteMerchandiseReturn",
                                                  Enabled = true,
                                                  Name = "Delete a merchandise return"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteSalesEmployee",
                                                  Enabled = true,
                                                  Name = "Delete a sales employee"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteSetting",
                                                  Enabled = true,
                                                  Name = "Delete a setting"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteSpecAttribute",
                                                  Enabled = true,
                                                  Name = "Delete a specification attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "DeletePage",
                                                  Enabled = true,
                                                  Name = "Delete a page"
                                              },
                                        new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteWidget",
                                                  Enabled = true,
                                                  Name = "Delete a widget"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCategory",
                                                  Enabled = true,
                                                  Name = "Edit category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCheckoutAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a checkout attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditContactAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a contact attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCustomer",
                                                  Enabled = true,
                                                  Name = "Edit a customer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCustomerGroup",
                                                  Enabled = true,
                                                  Name = "Edit a customer group"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditDiscount",
                                                  Enabled = true,
                                                  Name = "Edit a discount"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditDocument",
                                                  Enabled = false,
                                                  Name = "Edit document"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditDocumentType",
                                                  Enabled = false,
                                                  Name = "Edit document type"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditGiftVoucher",
                                                  Enabled = true,
                                                  Name = "Edit a gift voucher"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditCollection",
                                                  Enabled = true,
                                                  Name = "Edit a collection"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditOrder",
                                                  Enabled = true,
                                                  Name = "Edit an order"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditProduct",
                                                  Enabled = true,
                                                  Name = "Edit a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditProductAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a product attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditPromotionProviders",
                                                  Enabled = true,
                                                  Name = "Edit promotion providers"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditMerchandiseReturn",
                                                  Enabled = true,
                                                  Name = "Edit a merchandise return"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditSalesEmployee",
                                                  Enabled = true,
                                                  Name = "Edit a sales employee"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditSettings",
                                                  Enabled = true,
                                                  Name = "Edit setting(s)"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditSpecAttribute",
                                                  Enabled = true,
                                                  Name = "Edit a specification attribute"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "EditPage",
                                                  Enabled = true,
                                                  Name = "Edit a page"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "InteractiveFormDelete",
                                                  Enabled = true,
                                                  Name = "Delete a interactive form"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "InteractiveFormEdit",
                                                  Enabled = true,
                                                  Name = "Edit a interactive form"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "InteractiveFormAdd",
                                                  Enabled = true,
                                                  Name = "Add a interactive form"
                                              },
                                           new ActivityLogType
                                              {
                                                  SystemKeyword = "EditWidget",
                                                  Enabled = true,
                                                  Name = "Edit a widget"
                                              },
                                              //public store activities
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.Url",
                                                  Enabled = false,
                                                  Name = "Public store. Viewed Url"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ViewCategory",
                                                  Enabled = false,
                                                  Name = "Public store. View a category"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ViewCollection",
                                                  Enabled = false,
                                                  Name = "Public store. View a collection"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ViewProduct",
                                                  Enabled = false,
                                                  Name = "Public store. View a product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ViewCourse",
                                                  Enabled = false,
                                                  Name = "Public store. View a course"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ViewLesson",
                                                  Enabled = false,
                                                  Name = "Public store. View a lesson"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AskQuestion",
                                                  Enabled = false,
                                                  Name = "Public store. Ask a question about product"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.InteractiveForm",
                                                  Enabled = false,
                                                  Name = "Public store. Show interactive form"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.PlaceOrder",
                                                  Enabled = false,
                                                  Name = "Public store. Place an order"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.SendPM",
                                                  Enabled = false,
                                                  Name = "Public store. Send PM"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.ContactUs",
                                                  Enabled = false,
                                                  Name = "Public store. Use contact us form"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddNewBid",
                                                  Enabled = false,
                                                  Name = "Public store. Add new bid"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddToCompareList",
                                                  Enabled = false,
                                                  Name = "Public store. Add to compare list"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddToShoppingCart",
                                                  Enabled = false,
                                                  Name = "Public store. Add to shopping cart"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddToWishlist",
                                                  Enabled = false,
                                                  Name = "Public store. Add to wishlist"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.Login",
                                                  Enabled = false,
                                                  Name = "Public store. Login"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.Logout",
                                                  Enabled = false,
                                                  Name = "Public store. Logout"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddProductReview",
                                                  Enabled = false,
                                                  Name = "Public store. Add product review"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddNewsComment",
                                                  Enabled = false,
                                                  Name = "Public store. Add news comment"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddBlogComment",
                                                  Enabled = false,
                                                  Name = "Public store. Add blog comment"
                                              },
                                        new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.AddArticleComment",
                                                  Enabled = false,
                                                  Name = "Public store. Add article comment"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "PublicStore.DeleteAccount",
                                                  Enabled = false,
                                                  Name = "Public store. Delete account"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.AbandonedCart",
                                                  Enabled = true,
                                                  Name = "Send email Customer reminder - AbandonedCart"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.RegisteredCustomer",
                                                  Enabled = true,
                                                  Name = "Send email Customer reminder - RegisteredCustomer"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.LastActivity",
                                                  Enabled = true,
                                                  Name = "Send email Customer reminder - LastActivity"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.LastPurchase",
                                                  Enabled = true,
                                                  Name = "Send email Customer reminder - LastPurchase"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.Birthday",
                                                  Enabled = true,
                                                  Name = "Send email Customer reminder - Birthday"
                                              },
                                          new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerReminder.SendCampaign",
                                                  Enabled = true,
                                                  Name = "Send Campaign"
                                              },
                                           new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerAdmin.SendEmail",
                                                  Enabled = true,
                                                  Name = "Send email"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerAdmin.SendPM",
                                                  Enabled = true,
                                                  Name = "Send PM"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "CustomerAdmin.UpdateCartCustomer",
                                                  Enabled = true,
                                                  Name = "Update shopping cart"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "UpdateKnowledgebaseCategory",
                                                  Enabled = true,
                                                  Name = "Update knowledgebase category"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "CreateKnowledgebaseCategory",
                                                  Enabled = true,
                                                  Name = "Create knowledgebase category"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteKnowledgebaseCategory",
                                                  Enabled = true,
                                                  Name = "Delete knowledgebase category"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "CreateKnowledgebaseArticle",
                                                  Enabled = true,
                                                  Name = "Create knowledgebase article"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "UpdateKnowledgebaseArticle",
                                                  Enabled = true,
                                                  Name = "Update knowledgebase article"
                                              },
                                            new ActivityLogType
                                              {
                                                  SystemKeyword = "DeleteKnowledgebaseArticle",
                                                  Enabled = true,
                                                  Name = "Delete knowledgebase category"
                                              },
                                      };
            await _activityLogTypeRepository.InsertAsync(activityLogTypes);
        }
    }
}
